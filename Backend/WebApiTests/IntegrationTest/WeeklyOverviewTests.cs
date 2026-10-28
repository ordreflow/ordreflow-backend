using System.Net;
using System.Net.Http.Json;
using WebApiTests.IntegrationTest.Factories;
using WebAPI.Contracts.TimeEntries;

namespace WebApiTests.IntegrationTest;

// 7. Ugentligt overblik over registrerede timer.
public class WeeklyOverviewTests(TestWebAppFactory factory) : ApiTestBase(factory)
{
    private static async Task<WeeklyTotalResponse> GetTotalAsync(TestUser user, string from = WeekFrom, string to = WeekTo)
    {
        var total = await user.Client.GetFromJsonAsync<WeeklyTotalResponse>(
            $"/api/time_entries/weekly-total?fromDate={from}&toDate={to}");
        return total!;
    }

    [Fact]
    public async Task Weekly_total_is_zero_without_entries()
    {
        var s = await CreateScenarioAsync();

        var total = await GetTotalAsync(s.Employee);

        Assert.Equal(0m, total.TotalHours);
        Assert.Equal(new DateOnly(2026, 3, 2), total.FromDate);
        Assert.Equal(new DateOnly(2026, 3, 8), total.ToDate);
    }

    [Fact]
    public async Task Weekly_total_sums_entries_in_the_week()
    {
        var s = await CreateScenarioAsync();
        await RegisterTimeAsync(s.Employee, s.TaskId, Monday, 7.5m);
        await RegisterTimeAsync(s.Employee, s.TaskId, Monday.AddDays(1), 6m);
        await RegisterTimeAsync(s.Employee, s.TaskId, new DateTime(2026, 3, 8), 2.25m);

        var total = await GetTotalAsync(s.Employee);

        Assert.Equal(15.75m, total.TotalHours);
    }

    [Fact]
    public async Task Weekly_total_ignores_entries_outside_the_week()
    {
        var s = await CreateScenarioAsync();
        await RegisterTimeAsync(s.Employee, s.TaskId, Monday, 5m);
        await RegisterTimeAsync(s.Employee, s.TaskId, Monday.AddDays(-1), 8m);
        await RegisterTimeAsync(s.Employee, s.TaskId, Monday.AddDays(7), 8m);

        var total = await GetTotalAsync(s.Employee);

        Assert.Equal(5m, total.TotalHours);
    }

    [Fact]
    public async Task Weekly_total_only_counts_own_entries()
    {
        var s = await CreateScenarioAsync();
        var colleague = await CreateUserAsync(s.Admin, "Employee", s.Manager.Id);
        await RegisterTimeAsync(s.Employee, s.TaskId, Monday, 3m);
        await RegisterTimeAsync(colleague, s.TaskId, Monday, 9m);

        Assert.Equal(3m, (await GetTotalAsync(s.Employee)).TotalHours);
        Assert.Equal(9m, (await GetTotalAsync(colleague)).TotalHours);
    }

    [Fact]
    public async Task Weekly_total_includes_entries_in_all_statuses()
    {
        var s = await CreateScenarioAsync();
        var accepted = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 2m);
        var returned = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday.AddDays(1), 3m);
        await RegisterTimeAsync(s.Employee, s.TaskId, Monday.AddDays(2), 4m);
        await s.Manager.Client.PostAsync($"/api/time_entries/{accepted.Id}/accept", null);
        await ReturnEntryAsync(s.Manager, returned.Id, "check");

        var total = await GetTotalAsync(s.Employee);

        Assert.Equal(9m, total.TotalHours);
    }

    [Fact]
    public async Task Weekly_total_follows_updates_and_deletes()
    {
        var s = await CreateScenarioAsync();
        var first = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 2m);
        var second = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday.AddDays(1), 3m);

        await s.Employee.Client.PutAsJsonAsync($"/api/time_entries/{first.Id}",
            new UpdateTimeEntryRequest(Monday, 5m, null));
        await s.Employee.Client.DeleteAsync($"/api/time_entries/{second.Id}");

        Assert.Equal(5m, (await GetTotalAsync(s.Employee)).TotalHours);
    }

    [Fact]
    public async Task Weekly_total_with_end_before_start_is_rejected()
    {
        var s = await CreateScenarioAsync();

        var response = await s.Employee.Client.GetAsync(
            "/api/time_entries/weekly-total?fromDate=2026-03-08&toDate=2026-03-02");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Weekly_total_requires_login()
    {
        var response = await Anonymous().GetAsync(
            $"/api/time_entries/weekly-total?fromDate={WeekFrom}&toDate={WeekTo}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
