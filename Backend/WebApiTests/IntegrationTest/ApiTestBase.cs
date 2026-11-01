using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebApiTests.IntegrationTest.Factories;
using WebAPI.Contracts.Auth;
using WebAPI.Contracts.Orders;
using WebAPI.Contracts.Tasks;
using WebAPI.Contracts.TimeEntries;
using WebAPI.Contracts.Users;

namespace WebApiTests.IntegrationTest;

public sealed record TestUser(Guid Id, string Email, HttpClient Client);

// An admin, a manager and an employee of that manager, plus one open order with one open task.
public sealed record Scenario(
    TestUser Admin,
    TestUser Manager,
    TestUser Employee,
    Guid OrderId,
    Guid TaskId);

public abstract class ApiTestBase : IClassFixture<TestWebAppFactory>
{
    public const string Password = "Passw0rd!123";

    // A Monday, so a week is 2026-03-02 .. 2026-03-08.
    protected static readonly DateTime Monday = new(2026, 3, 2);
    protected const string WeekFrom = "2026-03-02";
    protected const string WeekTo = "2026-03-08";

    protected readonly TestWebAppFactory Factory;

    protected ApiTestBase(TestWebAppFactory factory) => Factory = factory;

    protected HttpClient Anonymous() => Factory.CreateClient();

    protected async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }

    protected async Task<TestUser> AdminAsync()
    {
        var client = await LoginAsync(TestWebAppFactory.AdminEmail, TestWebAppFactory.AdminPassword);
        var me = await client.GetFromJsonAsync<UserResponse>("/api/users/me");
        return new TestUser(me!.Id, me.Email, client);
    }

    protected async Task<TestUser> CreateUserAsync(
        TestUser admin,
        string role,
        Guid? managerId = null)
    {
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@test.local";
        var response = await admin.Client.PostAsJsonAsync("/api/users",
            new CreateUserRequest($"Test {role}", email, Password, role, managerId));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var users = await admin.Client.GetFromJsonAsync<ViewUsersResponse>("/api/users");
        var created = users!.Items.Single(user => user.Email == email);

        var client = await LoginAsync(email, Password);
        return new TestUser(created.Id, email, client);
    }

    protected async Task<Guid> CreateOrderAsync(TestUser manager, string? name = null)
    {
        name ??= "Order " + Guid.NewGuid().ToString("N");
        var response = await manager.Client.PostAsJsonAsync("/api/orders", new CreateOrderRequest(name));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var orders = await manager.Client.GetFromJsonAsync<ViewOrdersResponse>("/api/orders");
        return orders!.Items.Single(order => order.Name == name).Id;
    }

    protected async Task<Guid> CreateTaskAsync(TestUser manager, Guid orderId, string? title = null)
    {
        title ??= "Task " + Guid.NewGuid().ToString("N");
        var response = await manager.Client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest(orderId, title, "Description"));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var tasks = await manager.Client.GetFromJsonAsync<ViewTasksResponse>($"/api/tasks?orderId={orderId}");
        return tasks!.Items.Single(task => task.Title == title).Id;
    }

    protected async Task<Scenario> CreateScenarioAsync()
    {
        var admin = await AdminAsync();
        var manager = await CreateUserAsync(admin, "Manager");
        var employee = await CreateUserAsync(admin, "Employee", manager.Id);
        var orderId = await CreateOrderAsync(manager);
        var taskId = await CreateTaskAsync(manager, orderId);
        return new Scenario(admin, manager, employee, orderId, taskId);
    }

    protected static Task<HttpResponseMessage> RegisterTimeAsync(
        TestUser employee,
        Guid taskId,
        DateTime date,
        decimal hours,
        string? comment = null) =>
        employee.Client.PostAsJsonAsync("/api/time_entries",
            new CreateTimeEntryRequest(taskId, date, hours, comment));

    protected async Task<TimeEntryResponse> RegisterAndGetAsync(
        TestUser employee,
        Guid taskId,
        DateTime date,
        decimal hours,
        string? comment = null)
    {
        var response = await RegisterTimeAsync(employee, taskId, date, hours, comment);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var entries = await GetMyEntriesAsync(employee, WeekFrom, WeekTo);
        return entries.Single(entry => entry.Date.Date == date.Date && entry.Hours == hours);
    }

    protected static async Task<IReadOnlyCollection<TimeEntryResponse>> GetMyEntriesAsync(
        TestUser employee,
        string from,
        string to)
    {
        var result = await employee.Client.GetFromJsonAsync<ViewTimeEntriesResponse>(
            $"/api/time_entries?fromDate={from}&toDate={to}");
        return result!.Items;
    }

    protected static async Task<TimeEntryResponse> GetEntryAsync(TestUser user, Guid entryId)
    {
        var entry = await user.Client.GetFromJsonAsync<TimeEntryResponse>($"/api/time_entries/{entryId}");
        return entry!;
    }

    protected static Task<HttpResponseMessage> ReturnEntryAsync(TestUser manager, Guid entryId, string reason) =>
        manager.Client.PostAsJsonAsync($"/api/time_entries/{entryId}/return", new ReturnTimeEntryRequest(reason));
}
