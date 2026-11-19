using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Application.Interfaces;
using Domain.Aggregate;
using Domain.Entities;
using Task = System.Threading.Tasks.Task;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using WebApiTests.IntegrationTest.Factories;
using WebAPI.Contracts.Accounts;

namespace WebApiTests.IntegrationTest;

// UC-09 Find selectable and assigned work (FR-09, FR-21, FR-32, SEC-02, USE-04), through the real HTTP pipeline.
// Assignment links are seeded directly here so these tests focus on read-side filtering.
public sealed class FindWorkHttpTests(TestWebAppFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Employee_finds_general_work_and_only_their_own_assigned_work()
    {
        var w = await WorldAsync();

        var casey = await ListAsync(w.Casey);
        var jordan = await ListAsync(w.Jordan);

        Assert.Contains(w.General, casey.Keys);
        Assert.Contains(w.AssignedToCasey, casey.Keys);
        Assert.DoesNotContain(w.AssignedToJordan, casey.Keys);
        Assert.DoesNotContain(w.UnassignedPrivate, casey.Keys);
        Assert.DoesNotContain(w.General, (await ListAsync(w.Outsider)).Keys);

        Assert.Contains(w.AssignedToJordan, jordan.Keys);
        Assert.DoesNotContain(w.AssignedToCasey, jordan.Keys);

        var mine = casey[w.AssignedToCasey];
        Assert.True(mine.AssignedToMe);
        Assert.Equal((7.5m, new DateOnly(2026, 12, 1)), (mine.ExpectedHours, mine.ExpectedDeadline));
        Assert.StartsWith("Service", mine.DepartmentName);
        Assert.Equal("Customers", mine.CategoryName);
        Assert.False(casey[w.General].AssignedToMe);
        Assert.Null(casey[w.General].ExpectedHours);
    }

    // FR-32/USE-04: the filter narrows the authorised list, and turning it off restores general work.
    [Fact]
    public async Task Assigned_to_me_narrows_and_turning_it_off_restores_general_work()
    {
        var w = await WorldAsync();

        var assigned = await ListAsync(w.Casey, assignedToMe: true);
        var all = await ListAsync(w.Casey);

        Assert.Equal([w.AssignedToCasey], assigned.Keys.Where(w.All.Contains));
        Assert.All(assigned.Values, item => Assert.True(item.AssignedToMe));
        Assert.Contains(w.General, all.Keys);
        Assert.DoesNotContain(w.UnassignedPrivate, all.Keys);
        Assert.DoesNotContain(w.AssignedToJordan, all.Keys);
        // Assigned work is listed first.
        Assert.True(all.Values.First().AssignedToMe);
    }

    // FR-21: internal-only categories are for members of the owning department, also when work is assigned.
    [Fact]
    public async Task Internal_category_work_is_only_for_department_members()
    {
        var w = await WorldAsync();

        Assert.Contains(w.InternalGeneral, (await ListAsync(w.Casey)).Keys);
        Assert.DoesNotContain(w.InternalGeneral, (await ListAsync(w.Outsider)).Keys);
        Assert.DoesNotContain(w.InternalAssignedToOutsider, (await ListAsync(w.Outsider)).Keys);
        Assert.DoesNotContain(w.InternalAssignedToOutsider, (await ListAsync(w.Outsider, assignedToMe: true)).Keys);
    }

    // FR-21/SEC-02: hidden work cannot be found by direct URL or search, and looks like work that does not exist.
    [Fact]
    public async Task Hidden_work_cannot_be_reached_by_direct_url_or_search()
    {
        var w = await WorldAsync();

        Assert.Equal(HttpStatusCode.OK, (await w.Casey.GetAsync($"/api/work-items/{w.AssignedToCasey}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await w.Casey.GetAsync($"/api/work-items/{w.General}")).StatusCode);
        var hidden = await w.Casey.GetAsync($"/api/work-items/{w.AssignedToJordan}");
        var missing = await w.Casey.GetAsync($"/api/work-items/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.Equal(await missing.Content.ReadAsStringAsync(), await hidden.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await w.Casey.GetAsync($"/api/work-items/{w.UnassignedPrivate}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await w.Outsider.GetAsync($"/api/work-items/{w.General}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await w.Outsider.GetAsync($"/api/work-items/{w.InternalGeneral}")).StatusCode);

        Assert.Empty(await ListAsync(w.Casey, search: "Jordan only"));
        Assert.Empty(await ListAsync(w.Casey, search: "Nobody yet"));
        Assert.Equal([w.General], (await ListAsync(w.Casey, search: "  open DOOR ")).Keys.Where(w.All.Contains));
    }

    // USE-04: showing the category for context never exposes its private siblings.
    [Fact]
    public async Task Category_context_does_not_expose_private_siblings()
    {
        var w = await WorldAsync();

        var body = await w.Casey.GetStringAsync("/api/work-items");

        Assert.Contains("Open door", body);
        Assert.DoesNotContain("Jordan only", body);
        Assert.DoesNotContain("Nobody yet", body);
        Assert.DoesNotContain(w.JordanId.ToString(), body);
    }

    [Fact]
    public async Task Closed_work_and_closed_categories_are_not_selectable()
    {
        var w = await WorldAsync();
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var department = await context.Departments.Include(value => value.Catagories).ThenInclude(value => value.Tasks)
                .SingleAsync(value => value.Id == DepartmentId.Create(w.Service).Value);
            var category = department.Catagories.Single(value => value.Id == CatagoryId.Create(w.Customers).Value);
            Assert.True(department.CloseTask(category.Id, TaskId.Create(w.General).Value, DateTime.UtcNow.AddMinutes(1)).IsSuccess);
            await context.SaveChangesAsync();
        }

        Assert.DoesNotContain(w.General, (await ListAsync(w.Casey)).Keys);
        Assert.Equal(HttpStatusCode.NotFound, (await w.Casey.GetAsync($"/api/work-items/{w.General}")).StatusCode);
    }

    [Fact]
    public async Task Deactivated_employee_and_anonymous_callers_get_nothing()
    {
        var w = await WorldAsync();
        await w.Admin.PostAsync($"/api/employees/{w.JordanId}/deactivate", null);

        Assert.Equal(HttpStatusCode.Forbidden, (await w.Jordan.GetAsync("/api/work-items")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous().GetAsync("/api/work-items")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await w.Casey.GetAsync($"/api/work-items?search={new string('a', 201)}")).StatusCode);
    }

    // ---- world ----------------------------------------------------------------------------------------------

    private sealed record World(
        HttpClient Admin, HttpClient Casey, HttpClient Jordan, HttpClient Outsider, Guid JordanId,
        Guid Service, Guid Customers,
        Guid General, Guid AssignedToCasey, Guid AssignedToJordan, Guid UnassignedPrivate,
        Guid InternalGeneral, Guid InternalAssignedToOutsider)
    {
        public Guid[] All => [General, AssignedToCasey, AssignedToJordan, UnassignedPrivate, InternalGeneral, InternalAssignedToOutsider];
    }

    private async Task<World> WorldAsync()
    {
        var admin = await LoginAsync(TestWebAppFactory.AdminEmail, TestWebAppFactory.AdminPassword);
        var (caseyId, casey) = await EmployeeAsync(admin, "Casey");
        var (jordanId, jordan) = await EmployeeAsync(admin, "Jordan");
        var (outsiderId, outsider) = await EmployeeAsync(admin, "Outsider");
        var service = await CreatedAsync(admin.PostAsJsonAsync("/api/departments", new CreateDepartmentRequest("Service " + Guid.NewGuid().ToString("N"))));
        await admin.PutAsync($"/api/departments/{service}/employees/{caseyId}", null);
        await admin.PutAsync($"/api/departments/{service}/employees/{jordanId}", null);

        // The names are unique to this world so other tests' work never matches the searches.
        var tag = Guid.NewGuid().ToString("N")[..6];
        var customers = await CreatedAsync(admin.PostAsJsonAsync($"/api/departments/{service}/categories", new CreateCategoryRequest("Customers")));
        var internalCategory = await CreatedAsync(admin.PostAsJsonAsync($"/api/departments/{service}/categories", new CreateCategoryRequest("Internal", true)));
        Guid Work(Guid category, string name, bool assignmentOnly = false, string kind = "Task") =>
            CreatedAsync(admin.PostAsJsonAsync($"/api/categories/{category}/work-items",
                new CreateWorkItemRequest($"{name} {tag}", null, kind, assignmentOnly))).GetAwaiter().GetResult();

        var general = Work(customers, "Open door");
        var assignedToCasey = Work(customers, "Casey ticket", assignmentOnly: true, kind: "Ticket");
        var assignedToJordan = Work(customers, "Jordan only", assignmentOnly: true);
        var unassignedPrivate = Work(customers, "Nobody yet", assignmentOnly: true);
        var internalGeneral = Work(internalCategory, "Internal meeting");
        var internalAssigned = Work(internalCategory, "Outsider job", assignmentOnly: true);

        await AssignAsync(assignedToCasey, caseyId, 7.5m, new DateOnly(2026, 12, 1));
        await AssignAsync(assignedToJordan, jordanId, 2m, new DateOnly(2026, 12, 2));
        await AssignAsync(internalAssigned, outsiderId, 1m, new DateOnly(2026, 12, 3));

        return new World(admin, casey, jordan, outsider, jordanId, service, customers,
            general, assignedToCasey, assignedToJordan, unassignedPrivate, internalGeneral, internalAssigned);
    }

    private async Task<(Guid Id, HttpClient Client)> EmployeeAsync(HttpClient admin, string name)
    {
        var email = $"{name.ToLowerInvariant()}-{Guid.NewGuid():N}@test.local";
        var id = await CreatedAsync(admin.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest(name, email, Password)));
        return (id, await LoginAsync(email, Password));
    }

    private async Task AssignAsync(Guid task, Guid employee, decimal hours, DateOnly deadline)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.TaskAssinments.Add(new TaskAssignment
        {
            TaskId = TaskId.Create(task).Value,
            AssignedTo = EmployeeId.Create(employee).Value,
            ExpectedDuration = PlannedDuration.Create(hours).Value,
            ExpectedDeadline = TargetCompletionDate.Create(deadline).Value
        });
        await context.SaveChangesAsync();
    }

    private static async Task<Guid> CreatedAsync(Task<HttpResponseMessage> request)
    {
        var response = await request;
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
    }

    private static async Task<Dictionary<Guid, AccessibleWorkDto>> ListAsync(HttpClient client, bool assignedToMe = false, string? search = null)
    {
        var url = $"/api/work-items?assignedToMe={assignedToMe.ToString().ToLowerInvariant()}";
        if (search is not null) url += $"&search={Uri.EscapeDataString(search)}";
        var items = (await client.GetFromJsonAsync<ItemsResponse<AccessibleWorkDto>>(url))!.Items;
        return items.ToDictionary(item => item.Id);
    }
}

// PERF-01: the accessible-work list is filtered in the database and stays fast with many categories and tasks.
public sealed class FindWorkPerformanceTests(TestWebAppFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Accessible_work_for_a_large_company_is_listed_quickly_and_correctly()
    {
        var admin = await LoginAsync(TestWebAppFactory.AdminEmail, TestWebAppFactory.AdminPassword);
        var email = $"worker-{Guid.NewGuid():N}@test.local";
        var created = await admin.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest("Worker", email, Password));
        var workerId = EmployeeId.Create((await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id).Value;
        var worker = await LoginAsync(email, Password);

        const int departments = 10, categoriesPerDepartment = 10, tasksPerCategory = 30;
        var expected = 0;
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var creator = (await context.Employees.FirstAsync()).Id;
            for (var d = 0; d < departments; d++)
            {
                var department = Department.Create(DepartmentName.Create($"Perf {d} {Guid.NewGuid():N}").Value).Value;
                context.Departments.Add(department);
                if (d == 0) context.DepartmentEmployees.Add(new DepartmentEmployee { DepartmentId = department.Id, EmployeeId = workerId });
                for (var c = 0; c < categoriesPerDepartment; c++)
                {
                    var internalOnly = c % 2 == 1;
                    var category = department.AddCatagory(creator, CatagoryName.Create($"Category {c}").Value, internalOnly, DateTime.UtcNow).Value;
                    for (var t = 0; t < tasksPerCategory; t++)
                    {
                        var assignmentOnly = t % 3 == 0;
                        var task = department.AddTask(category.Id, TaskName.Create($"Task {t}").Value, DateTime.UtcNow, null,
                            WorkItemKind.Task, assignmentOnly).Value;
                        var assigned = assignmentOnly && t % 2 == 0;
                        if (assigned)
                            context.TaskAssinments.Add(new TaskAssignment { TaskId = task.Id, AssignedTo = workerId,
                                ExpectedDuration = PlannedDuration.Create(1m).Value,
                                ExpectedDeadline = TargetCompletionDate.Create(new DateOnly(2026, 12, 1)).Value });
                        var visibleCategory = !internalOnly || d == 0;
                        var inScope = d == 0 || assigned; // Work outside the worker's department is only for assignees.
                        if (visibleCategory && inScope && (!assignmentOnly || assigned)) expected++;
                    }
                }
            }
            await context.SaveChangesAsync();
        }

        var watch = Stopwatch.StartNew();
        var items = (await worker.GetFromJsonAsync<ItemsResponse<AccessibleWorkDto>>("/api/work-items"))!.Items;
        watch.Stop();

        Assert.Equal(expected, items.Count);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2), $"Listing took {watch.Elapsed}.");
    }
}
