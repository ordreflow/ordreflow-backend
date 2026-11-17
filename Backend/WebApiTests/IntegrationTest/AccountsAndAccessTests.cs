using System.Net;
using System.Net.Http.Json;
using WebApiTests.IntegrationTest.Factories;
using WebAPI.Contracts.Auth;
using WebAPI.Contracts.Departments;
using WebAPI.Contracts.TimeEntries;
using WebAPI.Contracts.Users;

namespace WebApiTests.IntegrationTest;

// UC-05: manage accounts and explicit access (FR-05, FR-13, FR-14, SEC-01).
public class AccountsAndAccessTests(TestWebAppFactory factory) : ApiTestBase(factory)
{
    // ---- FR-05: accounts --------------------------------------------------------------------------------------

    [Fact]
    public async Task Deactivated_user_cannot_sign_in_and_their_time_entries_are_kept()
    {
        var s = await CreateScenarioAsync();
        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 2m, "Before deactivation");

        var deactivate = await s.Admin.Client.DeleteAsync($"/api/users/{s.Employee.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);

        var login = await Anonymous().PostAsJsonAsync("/api/auth/login", new LoginRequest(s.Employee.Email, Password));
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);

        // The account is kept as inactive, and the manager still sees the historical entry.
        var user = await s.Admin.Client.GetFromJsonAsync<UserResponse>($"/api/users/{s.Employee.Id}");
        Assert.Equal("Inactive", user!.Status);
        var history = await s.Manager.Client.GetFromJsonAsync<ViewTimeEntriesResponse>(
            $"/api/time_entries/manager?fromDate={WeekFrom}&toDate={WeekTo}");
        Assert.Contains(history!.Items, item => item.Id == entry.Id && item.EmployeeId == s.Employee.Id);
    }

    // ---- FR-13: system access independent of job title --------------------------------------------------------

    [Fact]
    public async Task Admin_creates_user_with_job_title_and_job_title_grants_no_access()
    {
        var admin = await AdminAsync();
        var email = $"titled-{Guid.NewGuid():N}@test.local";

        var create = await admin.Client.PostAsJsonAsync("/api/users",
            new CreateUserRequest("Titled Employee", email, Password, "Employee", null, "  Manager  "));
        Assert.Equal(HttpStatusCode.NoContent, create.StatusCode);

        var employee = await LoginAsync(email, Password);
        var me = await employee.GetFromJsonAsync<UserResponse>("/api/users/me");
        Assert.Equal("Manager", me!.JobTitle);
        Assert.Equal("Employee", me.Role);

        // The "Manager" title does not open manager or admin endpoints.
        Assert.Equal(HttpStatusCode.Forbidden, (await employee.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await employee.GetAsync($"/api/time_entries/manager?fromDate={WeekFrom}&toDate={WeekTo}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await employee.GetAsync("/api/departments/managed-employees")).StatusCode);
    }

    [Fact]
    public async Task Admin_changes_and_clears_job_title_without_changing_role()
    {
        var admin = await AdminAsync();
        var manager = await CreateUserAsync(admin, "Manager");

        var set = await admin.Client.PostAsJsonAsync($"/api/users/{manager.Id}/jobTitle",
            new ChangeJobTitleRequest("Electrician"));
        Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);
        var afterSet = await admin.Client.GetFromJsonAsync<UserResponse>($"/api/users/{manager.Id}");
        Assert.Equal("Electrician", afterSet!.JobTitle);
        Assert.Equal("Manager", afterSet.Role);

        // A manager with a non-manager title keeps manager access.
        Assert.Equal(HttpStatusCode.OK, (await manager.Client.GetAsync("/api/users")).StatusCode);

        var clear = await admin.Client.PostAsJsonAsync($"/api/users/{manager.Id}/jobTitle",
            new ChangeJobTitleRequest(null));
        Assert.Equal(HttpStatusCode.NoContent, clear.StatusCode);
        var afterClear = await admin.Client.GetFromJsonAsync<UserResponse>($"/api/users/{manager.Id}");
        Assert.Null(afterClear!.JobTitle);
        Assert.Equal("Manager", afterClear.Role);
    }

    [Fact]
    public async Task Job_title_that_is_too_long_is_rejected()
    {
        var admin = await AdminAsync();
        var employee = await CreateUserAsync(admin, "Employee");

        var response = await admin.Client.PostAsJsonAsync($"/api/users/{employee.Id}/jobTitle",
            new ChangeJobTitleRequest(new string('a', 101)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Users_cannot_change_job_titles_including_their_own()
    {
        var admin = await AdminAsync();
        var manager = await CreateUserAsync(admin, "Manager");
        var employee = await CreateUserAsync(admin, "Employee", manager.Id);

        var own = await employee.Client.PostAsJsonAsync($"/api/users/{employee.Id}/jobTitle",
            new ChangeJobTitleRequest("Administrator"));
        var byManager = await manager.Client.PostAsJsonAsync($"/api/users/{employee.Id}/jobTitle",
            new ChangeJobTitleRequest("Manager"));

        Assert.Equal(HttpStatusCode.Forbidden, own.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, byManager.StatusCode);
        var user = await admin.Client.GetFromJsonAsync<UserResponse>($"/api/users/{employee.Id}");
        Assert.Null(user!.JobTitle);
    }

    // ---- FR-14: departments ---------------------------------------------------------------------------------

    [Fact]
    public async Task Admin_assigns_users_and_managers_to_several_departments_and_manager_scope_follows()
    {
        var admin = await AdminAsync();
        var manager = await CreateUserAsync(admin, "Manager");
        var otherManager = await CreateUserAsync(admin, "Manager");
        var inService = await CreateUserAsync(admin, "Employee");
        var inBoth = await CreateUserAsync(admin, "Employee");
        var elsewhere = await CreateUserAsync(admin, "Employee");

        var service = await CreateDepartmentAsync(admin);
        var installation = await CreateDepartmentAsync(admin);
        var sales = await CreateDepartmentAsync(admin);

        await SetMemberAsync(admin, service, manager.Id, isManager: true);
        await SetMemberAsync(admin, service, inService.Id);
        await SetMemberAsync(admin, service, inBoth.Id);
        await SetMemberAsync(admin, installation, manager.Id, isManager: true);
        await SetMemberAsync(admin, installation, inBoth.Id);
        await SetMemberAsync(admin, sales, otherManager.Id, isManager: true);
        await SetMemberAsync(admin, sales, elsewhere.Id);

        var managed = await ManagedEmployeesAsync(manager);
        Assert.Equal(new[] { inBoth.Id, inService.Id }.Order(), managed.Select(user => user.Id).Order());

        var otherManaged = await ManagedEmployeesAsync(otherManager);
        Assert.Equal(elsewhere.Id, Assert.Single(otherManaged).Id);

        // Removing a membership removes the employee from the manager's scope.
        var remove = await admin.Client.DeleteAsync($"/api/departments/{service}/members/{inService.Id}");
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);
        Assert.Equal(inBoth.Id, Assert.Single(await ManagedEmployeesAsync(manager)).Id);
    }

    [Fact]
    public async Task Department_members_are_only_visible_within_scope()
    {
        var admin = await AdminAsync();
        var manager = await CreateUserAsync(admin, "Manager");
        var employee = await CreateUserAsync(admin, "Employee");
        var colleague = await CreateUserAsync(admin, "Employee");
        var department = await CreateDepartmentAsync(admin);
        await SetMemberAsync(admin, department, manager.Id, isManager: true);
        await SetMemberAsync(admin, department, employee.Id);
        await SetMemberAsync(admin, department, colleague.Id);

        var asManager = await DepartmentsAsync(manager);
        var managerView = Assert.Single(asManager, item => item.Id == department);
        Assert.True(managerView.IsManagedByMe);
        Assert.Equal(3, managerView.Members.Count);
        Assert.True(managerView.Members.First().IsManager);

        var asEmployee = await DepartmentsAsync(employee);
        var employeeView = Assert.Single(asEmployee);
        Assert.False(employeeView.IsManagedByMe);
        Assert.Equal(employee.Id, Assert.Single(employeeView.Members).UserId);

        var asAdmin = await DepartmentsAsync(admin);
        Assert.Equal(3, Assert.Single(asAdmin, item => item.Id == department).Members.Count);
    }

    // SEC-01: an employee cannot be made department manager, whatever their job title.
    [Fact]
    public async Task Employee_with_manager_job_title_cannot_be_department_manager()
    {
        var admin = await AdminAsync();
        var employee = await CreateUserAsync(admin, "Employee");
        await admin.Client.PostAsJsonAsync($"/api/users/{employee.Id}/jobTitle", new ChangeJobTitleRequest("Manager"));
        var department = await CreateDepartmentAsync(admin);

        var response = await admin.Client.PutAsJsonAsync($"/api/departments/{department}/members/{employee.Id}",
            new SetDepartmentMemberRequest(true));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.Content.ReadAsStringAsync();
        Assert.Contains("DepartmentManagerAccessRequired", errors);
        Assert.Empty(Assert.Single(await DepartmentsAsync(admin), item => item.Id == department).Members);
    }

    // SEC-01: department scope requires manager access, so it ends when the access is removed.
    [Fact]
    public async Task Manager_demoted_to_employee_loses_department_scope()
    {
        var admin = await AdminAsync();
        var manager = await CreateUserAsync(admin, "Manager");
        var employee = await CreateUserAsync(admin, "Employee");
        var department = await CreateDepartmentAsync(admin);
        await SetMemberAsync(admin, department, manager.Id, isManager: true);
        await SetMemberAsync(admin, department, employee.Id);

        var demote = await admin.Client.PostAsJsonAsync($"/api/users/{manager.Id}/role",
            new ChangeUserRoleRequest("Employee"));
        Assert.Equal(HttpStatusCode.NoContent, demote.StatusCode);

        var relogged = await LoginAsync(manager.Email, Password);
        Assert.Equal(HttpStatusCode.Forbidden, (await relogged.GetAsync("/api/departments/managed-employees")).StatusCode);
        var view = Assert.Single(await DepartmentsAsync(relogged));
        Assert.False(view.IsManagedByMe);
        Assert.Equal(manager.Id, Assert.Single(view.Members).UserId);
    }

    [Fact]
    public async Task Deactivated_employee_leaves_manager_scope_but_keeps_department_history()
    {
        var admin = await AdminAsync();
        var manager = await CreateUserAsync(admin, "Manager");
        var employee = await CreateUserAsync(admin, "Employee");
        var department = await CreateDepartmentAsync(admin);
        await SetMemberAsync(admin, department, manager.Id, isManager: true);
        await SetMemberAsync(admin, department, employee.Id);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.Client.DeleteAsync($"/api/users/{employee.Id}")).StatusCode);

        Assert.Empty(await ManagedEmployeesAsync(manager));
        var member = Assert.Single(
            Assert.Single(await DepartmentsAsync(admin), item => item.Id == department).Members,
            item => item.UserId == employee.Id);
        Assert.Equal("Inactive", member.Status);
    }

    [Fact]
    public async Task Duplicate_or_blank_department_names_are_rejected()
    {
        var admin = await AdminAsync();
        var name = "Dept " + Guid.NewGuid().ToString("N");
        await CreateDepartmentAsync(admin, name);

        var duplicate = await admin.Client.PostAsJsonAsync("/api/departments", new CreateDepartmentRequest(name));
        var blank = await admin.Client.PostAsJsonAsync("/api/departments", new CreateDepartmentRequest("  "));

        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        Assert.Contains("DepartmentNameTaken", await duplicate.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
    }

    [Fact]
    public async Task Admin_renames_department()
    {
        var admin = await AdminAsync();
        var department = await CreateDepartmentAsync(admin);
        var newName = "Renamed " + Guid.NewGuid().ToString("N");

        var response = await admin.Client.PutAsJsonAsync($"/api/departments/{department}",
            new RenameDepartmentRequest(newName));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(newName, Assert.Single(await DepartmentsAsync(admin), item => item.Id == department).Name);
    }

    [Fact]
    public async Task Unknown_department_or_user_returns_404()
    {
        var admin = await AdminAsync();
        var employee = await CreateUserAsync(admin, "Employee");
        var department = await CreateDepartmentAsync(admin);

        var unknownDepartment = await admin.Client.PutAsJsonAsync(
            $"/api/departments/{Guid.NewGuid()}/members/{employee.Id}", new SetDepartmentMemberRequest(false));
        var unknownUser = await admin.Client.PutAsJsonAsync(
            $"/api/departments/{department}/members/{Guid.NewGuid()}", new SetDepartmentMemberRequest(false));
        var notMember = await admin.Client.DeleteAsync($"/api/departments/{department}/members/{employee.Id}");

        Assert.Equal(HttpStatusCode.NotFound, unknownDepartment.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownUser.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, notMember.StatusCode);
    }

    // SEC-01: the API itself refuses department administration by managers and employees, also via direct URLs.
    [Theory]
    [InlineData("Manager")]
    [InlineData("Employee")]
    public async Task Non_admins_cannot_administer_departments(string role)
    {
        var admin = await AdminAsync();
        var actor = await CreateUserAsync(admin, role);
        var employee = await CreateUserAsync(admin, "Employee");
        var department = await CreateDepartmentAsync(admin);

        var responses = new[]
        {
            await actor.Client.PostAsJsonAsync("/api/departments", new CreateDepartmentRequest("Mine")),
            await actor.Client.PutAsJsonAsync($"/api/departments/{department}", new RenameDepartmentRequest("Mine")),
            await actor.Client.PutAsJsonAsync($"/api/departments/{department}/members/{actor.Id}",
                new SetDepartmentMemberRequest(true)),
            await actor.Client.PutAsJsonAsync($"/api/departments/{department}/members/{employee.Id}",
                new SetDepartmentMemberRequest(false)),
            await actor.Client.DeleteAsync($"/api/departments/{department}/members/{employee.Id}"),
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
        Assert.Empty(Assert.Single(await DepartmentsAsync(admin), item => item.Id == department).Members);
    }

    [Theory]
    [InlineData("/api/departments")]
    [InlineData("/api/departments/managed-employees")]
    public async Task Department_endpoints_require_sign_in(string url)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonymous().GetAsync(url)).StatusCode);
    }

    // ---- helpers ----------------------------------------------------------------------------------------------

    private static async Task<Guid> CreateDepartmentAsync(TestUser admin, string? name = null)
    {
        name ??= "Dept " + Guid.NewGuid().ToString("N");
        var response = await admin.Client.PostAsJsonAsync("/api/departments", new CreateDepartmentRequest(name));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return (await DepartmentsAsync(admin)).Single(department => department.Name == name).Id;
    }

    private static async Task SetMemberAsync(TestUser admin, Guid departmentId, Guid userId, bool isManager = false)
    {
        var response = await admin.Client.PutAsJsonAsync($"/api/departments/{departmentId}/members/{userId}",
            new SetDepartmentMemberRequest(isManager));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static Task<IReadOnlyCollection<DepartmentResponse>> DepartmentsAsync(TestUser user) =>
        DepartmentsAsync(user.Client);

    private static async Task<IReadOnlyCollection<DepartmentResponse>> DepartmentsAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<ViewDepartmentsResponse>("/api/departments"))!.Items;

    private static async Task<IReadOnlyCollection<UserResponse>> ManagedEmployeesAsync(TestUser manager) =>
        (await manager.Client.GetFromJsonAsync<ViewUsersResponse>("/api/departments/managed-employees"))!.Items;
}
