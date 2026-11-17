using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.WorkPlanning;
using WebApiTests.IntegrationTest.Factories;
using WebAPI.Contracts.Accounts;

namespace WebApiTests.IntegrationTest;

// UC-08 Create work under a permitted category (FR-08, SEC-08), through the real HTTP pipeline.
public sealed class WorkPlanningHttpTests(TestWebAppFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Department_manager_creates_categories_and_work_in_a_managed_department()
    {
        var (admin, manager, department) = await ManagerOfNewDepartmentAsync();

        var category = await CreateCategoryAsync(manager, department, "Customers", internalOnly: true);
        var project = await CreateWorkAsync(manager, category, new CreateWorkItemRequest("New site", "Build it", "Project"));
        var ticket = await CreateWorkAsync(manager, category, new CreateWorkItemRequest(" Fix door ", null, "ticket", AssignmentOnly: true));
        var task = await CreateWorkAsync(manager, category, new CreateWorkItemRequest("Clean up"));

        var listed = Assert.Single(await PlannableAsync(manager), item => item.Id == category);
        Assert.True(listed.InternalOnly);
        Assert.Equal(department, listed.DepartmentId);
        var items = listed.WorkItems.ToDictionary(item => item.Id);
        Assert.Equal(("Project", false), (items[project].Kind, items[project].AssignmentOnly));
        Assert.Equal(("Fix door", "Ticket", true), (items[ticket].Name, items[ticket].Kind, items[ticket].AssignmentOnly));
        Assert.Equal(("Task", false), (items[task].Kind, items[task].AssignmentOnly));
        Assert.All(listed.WorkItems, item => Assert.Equal(category, item.CategoryId));

        // The administrator sees it too.
        Assert.Contains(await PlannableAsync(admin), item => item.Id == category);
    }

    [Fact]
    public async Task Manager_cannot_create_work_anywhere_else()
    {
        var (admin, manager, _) = await ManagerOfNewDepartmentAsync();
        var otherDepartment = await CreateDepartmentAsync(admin);
        var otherCategory = await CreateCategoryAsync(admin, otherDepartment, "Other");

        var category = await manager.PostAsJsonAsync($"/api/departments/{otherDepartment}/categories", new CreateCategoryRequest("Mine"));
        var work = await manager.PostAsJsonAsync($"/api/categories/{otherCategory}/work-items", new CreateWorkItemRequest("Sneaky"));

        Assert.Equal(HttpStatusCode.Forbidden, category.StatusCode);
        Assert.Equal("WorkPlanningForbidden", await ErrorAsync(category));
        Assert.Equal(HttpStatusCode.Forbidden, work.StatusCode);
        Assert.DoesNotContain(await PlannableAsync(manager), item => item.Id == otherCategory);
        Assert.Empty(Assert.Single(await PlannableAsync(admin), item => item.Id == otherCategory).WorkItems);
    }

    [Fact]
    public async Task Removing_the_grant_or_the_link_ends_planning_access()
    {
        var (admin, manager, department) = await ManagerOfNewDepartmentAsync();
        var category = await CreateCategoryAsync(manager, department, "Customers");
        var managerId = (await manager.GetFromJsonAsync<Application.Accounts.EmployeeDto>("/api/employees/me"))!.Id;

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/departments/{department}/managers/{managerId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await manager.PostAsJsonAsync($"/api/categories/{category}/work-items", new CreateWorkItemRequest("After"))).StatusCode);
        Assert.Empty(await PlannableAsync(manager));

        await admin.PutAsync($"/api/departments/{department}/managers/{managerId}", null);
        await admin.PutAsJsonAsync($"/api/employees/{managerId}/permissions", new SetEmployeePermissionsRequest(["Employee"]));
        Assert.Equal(HttpStatusCode.Forbidden,
            (await manager.PostAsJsonAsync($"/api/categories/{category}/work-items", new CreateWorkItemRequest("After"))).StatusCode);
    }

    [Theory]
    [InlineData("Employee")]
    [InlineData("Payroll")]
    [InlineData("CatagoryReviewer")]
    public async Task Others_cannot_plan_even_with_a_manager_title(string permission)
    {
        var admin = await AdminAsync();
        var department = await CreateDepartmentAsync(admin);
        var category = await CreateCategoryAsync(admin, department, "Customers");
        var email = NewEmail();
        var created = await admin.PostAsJsonAsync("/api/employees",
            new CreateEmployeeRequest("Titled", email, Password, "Department manager", [permission]));
        var id = (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
        await admin.PutAsync($"/api/departments/{department}/employees/{id}", null);
        var actor = await LoginAsync(email, Password);

        Assert.Equal(HttpStatusCode.Forbidden, (await actor.GetAsync("/api/categories/plannable")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await actor.PostAsJsonAsync($"/api/departments/{department}/categories", new CreateCategoryRequest("X"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await actor.PostAsJsonAsync($"/api/categories/{category}/work-items", new CreateWorkItemRequest("X"))).StatusCode);
    }

    [Fact]
    public async Task Validation_and_missing_resources()
    {
        var (_, manager, department) = await ManagerOfNewDepartmentAsync();
        var category = await CreateCategoryAsync(manager, department, "Customers");

        Assert.Equal("CatagoryNameRequired", await ErrorOrCodeAsync(manager.PostAsJsonAsync($"/api/departments/{department}/categories", new CreateCategoryRequest(" "))));
        Assert.Equal("TaskNameRequired", await ErrorOrCodeAsync(manager.PostAsJsonAsync($"/api/categories/{category}/work-items", new CreateWorkItemRequest(" "))));
        Assert.Equal("InvalidWorkItemKind", await ErrorOrCodeAsync(manager.PostAsJsonAsync($"/api/categories/{category}/work-items", new CreateWorkItemRequest("X", Kind: "Epic"))));
        Assert.Equal("InvalidWorkItemKind", await ErrorOrCodeAsync(manager.PostAsJsonAsync($"/api/categories/{category}/work-items", new CreateWorkItemRequest("X", Kind: "2"))));
        Assert.Equal("DescriptionTooLong", await ErrorOrCodeAsync(manager.PostAsJsonAsync($"/api/categories/{category}/work-items",
            new CreateWorkItemRequest("X", new string('a', 1001)))));
        Assert.Equal(HttpStatusCode.NotFound, (await manager.PostAsJsonAsync($"/api/categories/{Guid.NewGuid()}/work-items", new CreateWorkItemRequest("X"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await manager.PostAsJsonAsync($"/api/departments/{Guid.NewGuid()}/categories", new CreateCategoryRequest("X"))).StatusCode);
    }

    [Theory]
    [InlineData("/api/categories/plannable")]
    public async Task Planning_requires_sign_in(string url) =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous().GetAsync(url)).StatusCode);

    // ---- helpers ------------------------------------------------------------------------------------------

    private Task<HttpClient> AdminAsync() => LoginAsync(TestWebAppFactory.AdminEmail, TestWebAppFactory.AdminPassword);

    private static string NewEmail() => $"manager-{Guid.NewGuid():N}@test.local";

    private async Task<(HttpClient Admin, HttpClient Manager, Guid Department)> ManagerOfNewDepartmentAsync()
    {
        var admin = await AdminAsync();
        var email = NewEmail();
        var created = await admin.PostAsJsonAsync("/api/employees",
            new CreateEmployeeRequest("Morgan Manager", email, Password, null, ["DepartmentManager"]));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var managerId = (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
        var department = await CreateDepartmentAsync(admin);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsync($"/api/departments/{department}/managers/{managerId}", null)).StatusCode);
        return (admin, await LoginAsync(email, Password), department);
    }

    private static async Task<Guid> CreateDepartmentAsync(HttpClient admin)
    {
        var response = await admin.PostAsJsonAsync("/api/departments", new CreateDepartmentRequest("Dept " + Guid.NewGuid().ToString("N")));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
    }

    private static async Task<Guid> CreateCategoryAsync(HttpClient client, Guid department, string name, bool internalOnly = false)
    {
        var response = await client.PostAsJsonAsync($"/api/departments/{department}/categories", new CreateCategoryRequest(name, internalOnly));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
    }

    private static async Task<Guid> CreateWorkAsync(HttpClient client, Guid category, CreateWorkItemRequest request)
    {
        var response = await client.PostAsJsonAsync($"/api/categories/{category}/work-items", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
    }

    private static async Task<IReadOnlyList<PlannableCategoryDto>> PlannableAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<ItemsResponse<PlannableCategoryDto>>("/api/categories/plannable"))!.Items;

    private static async Task<string?> ErrorOrCodeAsync(Task<HttpResponseMessage> request) => await ErrorAsync(await request);

    private static async Task<string?> ErrorAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.EnumerateArray().First().GetProperty("code").GetString();
    }
}
