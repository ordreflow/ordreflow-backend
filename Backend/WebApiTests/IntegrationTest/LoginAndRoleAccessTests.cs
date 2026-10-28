using System.Net;
using System.Net.Http.Json;
using WebApiTests.IntegrationTest.Factories;
using WebAPI.Contracts.Auth;
using WebAPI.Contracts.Orders;
using WebAPI.Contracts.Users;

namespace WebApiTests.IntegrationTest;

// 1. Login og adgang baseret på brugerrolle.
public class LoginAndRoleAccessTests(TestWebAppFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Login_with_valid_credentials_returns_bearer_token()
    {
        var response = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new LoginRequest(TestWebAppFactory.AdminEmail, TestWebAppFactory.AdminPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(login!.AccessToken));
        Assert.Equal("Bearer", login.TokenType, ignoreCase: true);
        Assert.True(login.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var response = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new LoginRequest(TestWebAppFactory.AdminEmail, "Wrong-Password1!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_unknown_email_returns_401()
    {
        var response = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new LoginRequest("nobody@test.local", ApiTestBase.Password));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_as_deactivated_user_returns_403()
    {
        var admin = await AdminAsync();
        var employee = await CreateUserAsync(admin, "Employee");

        var deactivate = await admin.Client.DeleteAsync($"/api/users/{employee.Id}");
        Assert.True(deactivate.IsSuccessStatusCode);

        var response = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new LoginRequest(employee.Email, ApiTestBase.Password));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Health_is_available_without_token()
    {
        var response = await Anonymous().GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/orders")]
    [InlineData("/api/tasks")]
    [InlineData("/api/users")]
    [InlineData("/api/users/me")]
    [InlineData("/api/time_entries?fromDate=2026-03-02&toDate=2026-03-08")]
    public async Task Protected_endpoints_without_token_return_401(string url)
    {
        var response = await Anonymous().GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_token_returns_401()
    {
        var client = Anonymous();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "not-a-real-token");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_returns_the_logged_in_user_with_role()
    {
        var s = await CreateScenarioAsync();

        var me = await s.Employee.Client.GetFromJsonAsync<UserResponse>("/api/users/me");

        Assert.Equal(s.Employee.Id, me!.Id);
        Assert.Equal("Employee", me.Role);
        Assert.Equal(s.Manager.Id, me.ManagerId);
    }

    [Fact]
    public async Task Employee_cannot_use_manager_endpoints()
    {
        var s = await CreateScenarioAsync();

        var createOrder = await s.Employee.Client.PostAsJsonAsync("/api/orders", new CreateOrderRequest("Nope"));
        var managerEntries = await s.Employee.Client.GetAsync($"/api/time_entries/manager?fromDate={WeekFrom}&toDate={WeekTo}");
        var users = await s.Employee.Client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, createOrder.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, managerEntries.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, users.StatusCode);
    }

    [Fact]
    public async Task Manager_cannot_use_admin_endpoints()
    {
        var s = await CreateScenarioAsync();

        var create = await s.Manager.Client.PostAsJsonAsync("/api/users",
            new CreateUserRequest("X", "x@test.local", ApiTestBase.Password, "Employee"));
        var role = await s.Manager.Client.PostAsJsonAsync($"/api/users/{s.Employee.Id}/role",
            new ChangeUserRoleRequest("Admin"));
        var delete = await s.Manager.Client.DeleteAsync($"/api/users/{s.Employee.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, role.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Admin_can_use_manager_and_admin_endpoints()
    {
        var admin = await AdminAsync();

        var users = await admin.Client.GetAsync("/api/users");
        var orders = await admin.Client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, users.StatusCode);
        Assert.Equal(HttpStatusCode.OK, orders.StatusCode);
    }
}
