using System.Net;
using System.Net.Http.Json;
using WebApiTests.IntegrationTest.Factories;
using WebAPI.Contracts.Users;

namespace WebApiTests.IntegrationTest;

// 8. Administration af brugere og relevante relationer mellem medarbejdere og managers.
public class UserAdministrationTests(TestWebAppFactory factory) : ApiTestBase(factory)
{
    private static string NewEmail() => $"user-{Guid.NewGuid():N}@test.local";

    [Fact]
    public async Task Admin_creates_employee_with_manager()
    {
        var s = await CreateScenarioAsync();

        var user = await s.Admin.Client.GetFromJsonAsync<UserResponse>($"/api/users/{s.Employee.Id}");

        Assert.Equal("Employee", user!.Role);
        Assert.Equal("Active", user.Status);
        Assert.Equal(s.Manager.Id, user.ManagerId);
    }

    [Theory]
    [InlineData("Employee")]
    [InlineData("Manager")]
    [InlineData("Admin")]
    public async Task Admin_can_create_users_of_every_role(string role)
    {
        var admin = await AdminAsync();

        var user = await CreateUserAsync(admin, role);
        var me = await user.Client.GetFromJsonAsync<UserResponse>("/api/users/me");

        Assert.Equal(role, me!.Role);
    }

    [Fact]
    public async Task Created_user_can_log_in()
    {
        var admin = await AdminAsync();

        var user = await CreateUserAsync(admin, "Employee");

        Assert.Equal(HttpStatusCode.OK, (await user.Client.GetAsync("/api/users/me")).StatusCode);
    }

    [Fact]
    public async Task Creating_user_with_duplicate_email_is_rejected()
    {
        var admin = await AdminAsync();
        var existing = await CreateUserAsync(admin, "Employee");

        var response = await admin.Client.PostAsJsonAsync("/api/users",
            new CreateUserRequest("Dup", existing.Email, Password, "Employee"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("", "valid@test.local", "Passw0rd!123", "Employee")]
    [InlineData("Name", "not-an-email", "Passw0rd!123", "Employee")]
    [InlineData("Name", "valid@test.local", "", "Employee")]
    [InlineData("Name", "valid@test.local", "Passw0rd!123", "Wizard")]
    public async Task Creating_user_with_invalid_data_is_rejected(string name, string email, string password, string role)
    {
        var admin = await AdminAsync();

        var response = await admin.Client.PostAsJsonAsync("/api/users",
            new CreateUserRequest(name, email, password, role));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Creating_user_with_unknown_manager_is_rejected()
    {
        var admin = await AdminAsync();

        var response = await admin.Client.PostAsJsonAsync("/api/users",
            new CreateUserRequest("Name", NewEmail(), Password, "Employee", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_assigns_and_changes_manager()
    {
        var s = await CreateScenarioAsync();
        var newManager = await CreateUserAsync(s.Admin, "Manager");

        var response = await s.Admin.Client.PostAsJsonAsync($"/api/users/{s.Employee.Id}/manager",
            new AssignManagerRequest(newManager.Id));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var user = await s.Admin.Client.GetFromJsonAsync<UserResponse>($"/api/users/{s.Employee.Id}");
        Assert.Equal(newManager.Id, user!.ManagerId);
    }

    [Fact]
    public async Task Employee_cannot_be_assigned_as_manager()
    {
        var s = await CreateScenarioAsync();
        var colleague = await CreateUserAsync(s.Admin, "Employee");

        var response = await s.Admin.Client.PostAsJsonAsync($"/api/users/{colleague.Id}/manager",
            new AssignManagerRequest(s.Employee.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task User_cannot_be_own_manager()
    {
        var s = await CreateScenarioAsync();

        var response = await s.Admin.Client.PostAsJsonAsync($"/api/users/{s.Manager.Id}/manager",
            new AssignManagerRequest(s.Manager.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Non_admin_cannot_assign_manager()
    {
        var s = await CreateScenarioAsync();

        var response = await s.Manager.Client.PostAsJsonAsync($"/api/users/{s.Employee.Id}/manager",
            new AssignManagerRequest(s.Manager.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_changes_user_role_and_new_role_applies_after_login()
    {
        var s = await CreateScenarioAsync();

        var response = await s.Admin.Client.PostAsJsonAsync($"/api/users/{s.Employee.Id}/role",
            new ChangeUserRoleRequest("Manager"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var user = await s.Admin.Client.GetFromJsonAsync<UserResponse>($"/api/users/{s.Employee.Id}");
        Assert.Equal("Manager", user!.Role);

        var relogin = await LoginAsync(s.Employee.Email, Password);
        Assert.Equal(HttpStatusCode.OK, (await relogin.GetAsync("/api/users")).StatusCode);
    }

    [Fact]
    public async Task Changing_to_invalid_role_is_rejected()
    {
        var s = await CreateScenarioAsync();

        var response = await s.Admin.Client.PostAsJsonAsync($"/api/users/{s.Employee.Id}/role",
            new ChangeUserRoleRequest("Wizard"));

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Admin_sees_all_users_with_manager_relations()
    {
        var s = await CreateScenarioAsync();

        var users = await s.Admin.Client.GetFromJsonAsync<ViewUsersResponse>("/api/users");

        Assert.Contains(users!.Items, u => u.Id == s.Manager.Id && u.Role == "Manager");
        Assert.Contains(users.Items, u => u.Id == s.Employee.Id && u.ManagerId == s.Manager.Id);
    }

    [Fact]
    public async Task Manager_sees_self_and_own_employees_only()
    {
        var s = await CreateScenarioAsync();
        var otherManager = await CreateUserAsync(s.Admin, "Manager");
        var foreign = await CreateUserAsync(s.Admin, "Employee", otherManager.Id);

        var users = await s.Manager.Client.GetFromJsonAsync<ViewUsersResponse>("/api/users");

        Assert.Contains(users!.Items, u => u.Id == s.Manager.Id);
        Assert.Contains(users.Items, u => u.Id == s.Employee.Id);
        Assert.DoesNotContain(users.Items, u => u.Id == foreign.Id);
        Assert.DoesNotContain(users.Items, u => u.Id == otherManager.Id);
    }

    [Fact]
    public async Task Manager_can_view_own_employee_but_not_foreign_employee()
    {
        var s = await CreateScenarioAsync();
        var otherManager = await CreateUserAsync(s.Admin, "Manager");
        var foreign = await CreateUserAsync(s.Admin, "Employee", otherManager.Id);

        var own = await s.Manager.Client.GetAsync($"/api/users/{s.Employee.Id}");
        var other = await s.Manager.Client.GetAsync($"/api/users/{foreign.Id}");

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.False(other.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Employee_cannot_view_other_users()
    {
        var s = await CreateScenarioAsync();

        var response = await s.Employee.Client.GetAsync($"/api/users/{s.Manager.Id}");

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task User_can_edit_own_profile()
    {
        var s = await CreateScenarioAsync();
        var newEmail = NewEmail();

        var response = await s.Employee.Client.PutAsJsonAsync($"/api/users/{s.Employee.Id}",
            new UpdateUserRequest("New Name", newEmail));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var me = await s.Employee.Client.GetFromJsonAsync<UserResponse>("/api/users/me");
        Assert.Equal("New Name", me!.Name);
        Assert.Equal(newEmail, me.Email);
    }

    [Fact]
    public async Task Employee_cannot_edit_another_users_profile()
    {
        var s = await CreateScenarioAsync();

        var response = await s.Employee.Client.PutAsJsonAsync($"/api/users/{s.Manager.Id}",
            new UpdateUserRequest("Hacked", NewEmail()));

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Admin_deactivates_user_and_user_can_no_longer_log_in()
    {
        var s = await CreateScenarioAsync();

        var response = await s.Admin.Client.DeleteAsync($"/api/users/{s.Employee.Id}");

        Assert.True(response.IsSuccessStatusCode);
        var user = await s.Admin.Client.GetFromJsonAsync<UserResponse>($"/api/users/{s.Employee.Id}");
        Assert.Equal("Inactive", user!.Status);

        var login = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new WebAPI.Contracts.Auth.LoginRequest(s.Employee.Email, Password));
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);
    }

    [Fact]
    public async Task Deactivating_twice_is_rejected()
    {
        var s = await CreateScenarioAsync();
        await s.Admin.Client.DeleteAsync($"/api/users/{s.Employee.Id}");

        var response = await s.Admin.Client.DeleteAsync($"/api/users/{s.Employee.Id}");

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Deactivated_employee_cannot_register_time()
    {
        var s = await CreateScenarioAsync();
        await s.Admin.Client.DeleteAsync($"/api/users/{s.Employee.Id}");

        var response = await RegisterTimeAsync(s.Employee, s.TaskId, Monday, 1m);

        Assert.False(response.IsSuccessStatusCode);
    }
}
