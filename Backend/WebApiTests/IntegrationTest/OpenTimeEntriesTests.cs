using System.Net;
using System.Net.Http.Json;
using WebApiTests.IntegrationTest.Factories;
using WebAPI.Contracts.TimeEntries;

namespace WebApiTests.IntegrationTest;

// 4. Medarbejderens visning og redigering af relevante åbne registreringer.
public class OpenTimeEntriesTests(TestWebAppFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Employee_sees_only_own_entries_in_the_requested_period()
    {
        var s = await CreateScenarioAsync();
        var colleague = await CreateUserAsync(s.Admin, "Employee", s.Manager.Id);
        var inWeek = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday.AddDays(1), 3m);
        await RegisterTimeAsync(s.Employee, s.TaskId, Monday.AddDays(-7), 4m);
        await RegisterTimeAsync(colleague, s.TaskId, Monday.AddDays(1), 5m);

        var entries = await GetMyEntriesAsync(s.Employee, WeekFrom, WeekTo);

        var only = Assert.Single(entries);
        Assert.Equal(inWeek.Id, only.Id);
    }

    [Fact]
    public async Task Period_end_date_is_inclusive()
    {
        var s = await CreateScenarioAsync();
        await RegisterTimeAsync(s.Employee, s.TaskId, new DateTime(2026, 3, 8), 1m);

        var entries = await GetMyEntriesAsync(s.Employee, WeekFrom, WeekTo);

        Assert.Single(entries);
    }

    [Fact]
    public async Task Entries_are_ordered_by_date()
    {
        var s = await CreateScenarioAsync();
        await RegisterTimeAsync(s.Employee, s.TaskId, Monday.AddDays(3), 1m);
        await RegisterTimeAsync(s.Employee, s.TaskId, Monday, 1m);

        var entries = (await GetMyEntriesAsync(s.Employee, WeekFrom, WeekTo)).ToList();

        Assert.Equal(entries.OrderBy(entry => entry.Date).Select(entry => entry.Id), entries.Select(entry => entry.Id));
    }

    [Fact]
    public async Task Employee_can_view_single_entry()
    {
        var s = await CreateScenarioAsync();
        var created = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 2m);

        var entry = await GetEntryAsync(s.Employee, created.Id);

        Assert.Equal(created.Id, entry.Id);
    }

    [Fact]
    public async Task Employee_cannot_view_entry_of_another_employee()
    {
        var s = await CreateScenarioAsync();
        var colleague = await CreateUserAsync(s.Admin, "Employee", s.Manager.Id);
        var created = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 2m);

        var response = await colleague.Client.GetAsync($"/api/time_entries/{created.Id}");

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Employee_can_update_open_entry()
    {
        var s = await CreateScenarioAsync();
        var created = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 2m, "old");

        var response = await s.Employee.Client.PutAsJsonAsync($"/api/time_entries/{created.Id}",
            new UpdateTimeEntryRequest(Monday.AddDays(1), 6m, "new"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await GetEntryAsync(s.Employee, created.Id);
        Assert.Equal(6m, updated.Hours);
        Assert.Equal("new", updated.Comment);
        Assert.Equal(Monday.AddDays(1).Date, updated.Date.Date);
        Assert.Equal("Draft", updated.Status);
    }

    [Fact]
    public async Task Update_with_invalid_hours_is_rejected()
    {
        var s = await CreateScenarioAsync();
        var created = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 2m);

        var response = await s.Employee.Client.PutAsJsonAsync($"/api/time_entries/{created.Id}",
            new UpdateTimeEntryRequest(Monday, 25m, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(2m, (await GetEntryAsync(s.Employee, created.Id)).Hours);
    }

    [Fact]
    public async Task Employee_cannot_update_entry_of_another_employee()
    {
        var s = await CreateScenarioAsync();
        var colleague = await CreateUserAsync(s.Admin, "Employee", s.Manager.Id);
        var created = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 2m);

        var response = await colleague.Client.PutAsJsonAsync($"/api/time_entries/{created.Id}",
            new UpdateTimeEntryRequest(Monday, 9m, "hijack"));

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal(2m, (await GetEntryAsync(s.Employee, created.Id)).Hours);
    }

    [Fact]
    public async Task Accepted_entry_can_no_longer_be_updated()
    {
        var s = await CreateScenarioAsync();
        var created = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 2m);
        var accept = await s.Manager.Client.PostAsync($"/api/time_entries/{created.Id}/accept", null);
        Assert.True(accept.IsSuccessStatusCode);

        var response = await s.Employee.Client.PutAsJsonAsync($"/api/time_entries/{created.Id}",
            new UpdateTimeEntryRequest(Monday, 3m, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Employee_can_delete_open_entry()
    {
        var s = await CreateScenarioAsync();
        var created = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 2m);

        var response = await s.Employee.Client.DeleteAsync($"/api/time_entries/{created.Id}");

        Assert.True(response.IsSuccessStatusCode);
        Assert.Empty(await GetMyEntriesAsync(s.Employee, WeekFrom, WeekTo));
    }

    [Fact]
    public async Task Employee_cannot_delete_entry_of_another_employee()
    {
        var s = await CreateScenarioAsync();
        var colleague = await CreateUserAsync(s.Admin, "Employee", s.Manager.Id);
        var created = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 2m);

        var response = await colleague.Client.DeleteAsync($"/api/time_entries/{created.Id}");

        Assert.False(response.IsSuccessStatusCode);
        Assert.Single(await GetMyEntriesAsync(s.Employee, WeekFrom, WeekTo));
    }
}
