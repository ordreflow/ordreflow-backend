using System.Net;
using System.Net.Http.Json;
using WebApiTests.IntegrationTest.Factories;
using WebAPI.Contracts.TimeEntries;

namespace WebApiTests.IntegrationTest;

// 6. Medarbejderens mulighed for at rette og genindsende returnerede timer.
public class ReturnedTimeEntriesTests(TestWebAppFactory factory) : ApiTestBase(factory)
{
    private async Task<(Scenario Scenario, TimeEntryResponse Entry)> ReturnedEntryAsync()
    {
        var s = await CreateScenarioAsync();
        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 4m, "first try");
        var response = await ReturnEntryAsync(s.Manager, entry.Id, "Wrong hours");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        return (s, entry);
    }

    [Fact]
    public async Task Employee_sees_returned_entry_in_own_list()
    {
        var (s, entry) = await ReturnedEntryAsync();

        var entries = await GetMyEntriesAsync(s.Employee, WeekFrom, WeekTo);

        Assert.Contains(entries, e => e.Id == entry.Id && e.Status == "Returned");
    }

    [Fact]
    public async Task Employee_can_correct_returned_entry()
    {
        var (s, entry) = await ReturnedEntryAsync();

        var response = await s.Employee.Client.PutAsJsonAsync($"/api/time_entries/{entry.Id}",
            new UpdateTimeEntryRequest(Monday, 6m, "corrected"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await GetEntryAsync(s.Employee, entry.Id);
        Assert.Equal(6m, updated.Hours);
        Assert.Equal("corrected", updated.Comment);
        Assert.Equal("Returned", updated.Status);
    }

    [Fact]
    public async Task Employee_resubmits_returned_entry_and_it_becomes_open_again()
    {
        var (s, entry) = await ReturnedEntryAsync();
        await s.Employee.Client.PutAsJsonAsync($"/api/time_entries/{entry.Id}",
            new UpdateTimeEntryRequest(Monday, 6m, "corrected"));

        var response = await s.Employee.Client.PostAsync($"/api/time_entries/{entry.Id}/resubmit", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("Draft", (await GetEntryAsync(s.Employee, entry.Id)).Status);
    }

    [Fact]
    public async Task Resubmitted_entry_can_be_accepted_by_manager()
    {
        var (s, entry) = await ReturnedEntryAsync();
        await s.Employee.Client.PostAsync($"/api/time_entries/{entry.Id}/resubmit", null);

        var accept = await s.Manager.Client.PostAsync($"/api/time_entries/{entry.Id}/accept", null);

        Assert.Equal(HttpStatusCode.NoContent, accept.StatusCode);
        Assert.Equal("Accepted", (await GetEntryAsync(s.Employee, entry.Id)).Status);
    }

    [Fact]
    public async Task Review_history_is_kept_after_resubmit()
    {
        var (s, entry) = await ReturnedEntryAsync();
        await s.Employee.Client.PostAsync($"/api/time_entries/{entry.Id}/resubmit", null);
        await s.Manager.Client.PostAsync($"/api/time_entries/{entry.Id}/accept", null);

        var reviews = await s.Manager.Client.GetFromJsonAsync<GetTimeEntryReviewsResponse>(
            $"/api/time_entries/{entry.Id}/reviews");

        Assert.Equal(2, reviews!.Items.Count);
        Assert.Contains(reviews.Items, r => r.Decision == "Returned" && r.Reason == "Wrong hours");
        Assert.Contains(reviews.Items, r => r.Decision == "Accepted");
    }

    [Fact]
    public async Task Open_entry_cannot_be_resubmitted()
    {
        var s = await CreateScenarioAsync();
        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 4m);

        var response = await s.Employee.Client.PostAsync($"/api/time_entries/{entry.Id}/resubmit", null);

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal("Draft", (await GetEntryAsync(s.Employee, entry.Id)).Status);
    }

    [Fact]
    public async Task Other_employee_cannot_resubmit_returned_entry()
    {
        var (s, entry) = await ReturnedEntryAsync();
        var colleague = await CreateUserAsync(s.Admin, "Employee", s.Manager.Id);

        var response = await colleague.Client.PostAsync($"/api/time_entries/{entry.Id}/resubmit", null);

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal("Returned", (await GetEntryAsync(s.Employee, entry.Id)).Status);
    }

    [Fact]
    public async Task Manager_cannot_resubmit_entry_on_behalf_of_employee()
    {
        var (s, entry) = await ReturnedEntryAsync();

        var response = await s.Manager.Client.PostAsync($"/api/time_entries/{entry.Id}/resubmit", null);

        Assert.False(response.IsSuccessStatusCode);
    }
}
