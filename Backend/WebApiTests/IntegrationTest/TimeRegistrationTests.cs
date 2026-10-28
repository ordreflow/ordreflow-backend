using System.Net;
using System.Net.Http.Json;
using WebApiTests.IntegrationTest.Factories;

namespace WebApiTests.IntegrationTest;

// 3. Medarbejderens registrering af timer.
public class TimeRegistrationTests(TestWebAppFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Employee_registers_time_and_entry_starts_as_draft()
    {
        var s = await CreateScenarioAsync();

        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 7.5m, "Worked on it");

        Assert.Equal("Draft", entry.Status);
        Assert.Equal(s.Employee.Id, entry.EmployeeId);
        Assert.Equal(s.TaskId, entry.TaskId);
        Assert.Equal(7.5m, entry.Hours);
        Assert.Equal("Worked on it", entry.Comment);
    }

    [Fact]
    public async Task Comment_is_optional()
    {
        var s = await CreateScenarioAsync();

        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 2m);

        Assert.Null(entry.Comment);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(24.5)]
    public async Task Invalid_hours_are_rejected(double hours)
    {
        var s = await CreateScenarioAsync();

        var response = await RegisterTimeAsync(s.Employee, s.TaskId, Monday, (decimal)hours);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Comment_over_1000_characters_is_rejected()
    {
        var s = await CreateScenarioAsync();

        var response = await RegisterTimeAsync(s.Employee, s.TaskId, Monday, 1m, new string('a', 1001));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_task_is_rejected()
    {
        var s = await CreateScenarioAsync();

        var response = await RegisterTimeAsync(s.Employee, Guid.NewGuid(), Monday, 1m);

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Time_cannot_be_registered_on_closed_task()
    {
        var s = await CreateScenarioAsync();
        await s.Manager.Client.PostAsync($"/api/tasks/{s.TaskId}/close", null);

        var response = await RegisterTimeAsync(s.Employee, s.TaskId, Monday, 1m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Time_cannot_be_registered_on_closed_order()
    {
        var s = await CreateScenarioAsync();
        await s.Manager.Client.PostAsync($"/api/orders/{s.OrderId}/close", null);

        var response = await RegisterTimeAsync(s.Employee, s.TaskId, Monday, 1m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Employee_of_another_manager_cannot_register_on_the_task()
    {
        var s = await CreateScenarioAsync();
        var otherManager = await CreateUserAsync(s.Admin, "Manager");
        var outsider = await CreateUserAsync(s.Admin, "Employee", otherManager.Id);

        var response = await RegisterTimeAsync(outsider, s.TaskId, Monday, 1m);

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Employee_without_manager_cannot_register_time()
    {
        var s = await CreateScenarioAsync();
        var loner = await CreateUserAsync(s.Admin, "Employee");

        var response = await RegisterTimeAsync(loner, s.TaskId, Monday, 1m);

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Unauthenticated_user_cannot_register_time()
    {
        var s = await CreateScenarioAsync();

        var response = await Anonymous().PostAsJsonAsync("/api/time_entries",
            new WebAPI.Contracts.TimeEntries.CreateTimeEntryRequest(s.TaskId, Monday, 1m, null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
