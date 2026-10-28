using System.Net;
using System.Net.Http.Json;
using WebApiTests.IntegrationTest.Factories;
using WebAPI.Contracts.TimeEntries;

namespace WebApiTests.IntegrationTest;

// 5. Managerens gennemgang, godkendelse og returnering af timer.
public class ManagerReviewTests(TestWebAppFactory factory) : ApiTestBase(factory)
{
    private static async Task<IReadOnlyCollection<TimeEntryResponse>> GetManagerEntriesAsync(
        TestUser manager,
        string extraQuery = "")
    {
        var result = await manager.Client.GetFromJsonAsync<ViewTimeEntriesResponse>(
            $"/api/time_entries/manager?fromDate={WeekFrom}&toDate={WeekTo}{extraQuery}");
        return result!.Items;
    }

    [Fact]
    public async Task Manager_sees_entries_of_own_employees_only()
    {
        var s = await CreateScenarioAsync();
        var otherManager = await CreateUserAsync(s.Admin, "Manager");
        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 4m);

        var own = await GetManagerEntriesAsync(s.Manager);
        var other = await GetManagerEntriesAsync(otherManager);

        Assert.Contains(own, e => e.Id == entry.Id);
        Assert.DoesNotContain(other, e => e.Id == entry.Id);
    }

    [Fact]
    public async Task Manager_can_view_entry_and_its_reviews()
    {
        var s = await CreateScenarioAsync();
        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 4m);
        await s.Manager.Client.PostAsync($"/api/time_entries/{entry.Id}/accept", null);

        var viewed = await GetEntryAsync(s.Manager, entry.Id);
        var reviews = await s.Manager.Client.GetFromJsonAsync<GetTimeEntryReviewsResponse>(
            $"/api/time_entries/{entry.Id}/reviews");

        Assert.Equal(entry.Id, viewed.Id);
        var review = Assert.Single(reviews!.Items);
        Assert.Equal("Accepted", review.Decision);
    }

    [Fact]
    public async Task Manager_accepts_entry()
    {
        var s = await CreateScenarioAsync();
        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 4m);

        var response = await s.Manager.Client.PostAsync($"/api/time_entries/{entry.Id}/accept", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("Accepted", (await GetEntryAsync(s.Employee, entry.Id)).Status);
    }

    [Fact]
    public async Task Manager_sees_status_of_each_entry()
    {
        var s = await CreateScenarioAsync();
        var accepted = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 4m);
        var draft = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday.AddDays(1), 2m);
        await s.Manager.Client.PostAsync($"/api/time_entries/{accepted.Id}/accept", null);

        var all = await GetManagerEntriesAsync(s.Manager);

        Assert.Contains(all, e => e.Id == accepted.Id && e.Status == "Accepted");
        Assert.Contains(all, e => e.Id == draft.Id && e.Status == "Draft");
    }

    [Fact]
    public async Task Manager_returns_entry_with_reason_and_review_is_recorded()
    {
        var s = await CreateScenarioAsync();
        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 4m);

        var response = await ReturnEntryAsync(s.Manager, entry.Id, "Please add a comment");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("Returned", (await GetEntryAsync(s.Employee, entry.Id)).Status);

        var reviews = await s.Employee.Client.GetFromJsonAsync<GetTimeEntryReviewsResponse>(
            $"/api/time_entries/{entry.Id}/reviews");
        var review = Assert.Single(reviews!.Items);
        Assert.Equal("Returned", review.Decision);
        Assert.Equal("Please add a comment", review.Reason);
    }

    [Fact]
    public async Task Return_without_reason_is_rejected()
    {
        var s = await CreateScenarioAsync();
        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 4m);

        var response = await ReturnEntryAsync(s.Manager, entry.Id, "");

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal("Draft", (await GetEntryAsync(s.Employee, entry.Id)).Status);
    }

    [Fact]
    public async Task Manager_finalizes_accepted_entry()
    {
        var s = await CreateScenarioAsync();
        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 4m);
        await s.Manager.Client.PostAsync($"/api/time_entries/{entry.Id}/accept", null);

        var response = await s.Manager.Client.PostAsync($"/api/time_entries/{entry.Id}/finalize", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("Finalized", (await GetEntryAsync(s.Employee, entry.Id)).Status);
    }

    [Fact]
    public async Task Draft_entry_cannot_be_finalized()
    {
        var s = await CreateScenarioAsync();
        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 4m);

        var response = await s.Manager.Client.PostAsync($"/api/time_entries/{entry.Id}/finalize", null);

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Accepted_entry_cannot_be_accepted_or_returned_again()
    {
        var s = await CreateScenarioAsync();
        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 4m);
        await s.Manager.Client.PostAsync($"/api/time_entries/{entry.Id}/accept", null);

        var accept = await s.Manager.Client.PostAsync($"/api/time_entries/{entry.Id}/accept", null);
        var returned = await ReturnEntryAsync(s.Manager, entry.Id, "Too late");

        Assert.False(accept.IsSuccessStatusCode);
        Assert.False(returned.IsSuccessStatusCode);
    }

    [Theory]
    [InlineData("accept", "Accepted")]
    [InlineData("return", "Returned")]
    public async Task Review_endpoint_applies_decision(string decision, string expectedStatus)
    {
        var s = await CreateScenarioAsync();
        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 4m);

        var response = await s.Manager.Client.PostAsJsonAsync($"/api/time_entries/{entry.Id}/review",
            new ReviewTimeEntryRequest(decision, "Needs work"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(expectedStatus, (await GetEntryAsync(s.Employee, entry.Id)).Status);
    }

    [Fact]
    public async Task Review_endpoint_rejects_unknown_decision()
    {
        var s = await CreateScenarioAsync();
        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 4m);

        var response = await s.Manager.Client.PostAsJsonAsync($"/api/time_entries/{entry.Id}/review",
            new ReviewTimeEntryRequest("maybe", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Manager_cannot_review_entries_of_other_managers_employees()
    {
        var s = await CreateScenarioAsync();
        var otherManager = await CreateUserAsync(s.Admin, "Manager");
        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 4m);

        var accept = await otherManager.Client.PostAsync($"/api/time_entries/{entry.Id}/accept", null);
        var returned = await ReturnEntryAsync(otherManager, entry.Id, "Not mine");

        Assert.False(accept.IsSuccessStatusCode);
        Assert.False(returned.IsSuccessStatusCode);
        Assert.Equal("Draft", (await GetEntryAsync(s.Employee, entry.Id)).Status);
    }

    [Fact]
    public async Task Admin_can_review_any_entry()
    {
        var s = await CreateScenarioAsync();
        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 4m);

        var response = await s.Admin.Client.PostAsync($"/api/time_entries/{entry.Id}/accept", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Employee_cannot_review_entries()
    {
        var s = await CreateScenarioAsync();
        var colleague = await CreateUserAsync(s.Admin, "Employee", s.Manager.Id);
        var entry = await RegisterAndGetAsync(s.Employee, s.TaskId, Monday, 4m);

        var own = await s.Employee.Client.PostAsync($"/api/time_entries/{entry.Id}/accept", null);
        var other = await colleague.Client.PostAsync($"/api/time_entries/{entry.Id}/accept", null);
        var returned = await ReturnEntryAsync(s.Employee, entry.Id, "self");

        Assert.Equal(HttpStatusCode.Forbidden, own.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, other.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, returned.StatusCode);
    }

    [Fact]
    public async Task Review_of_unknown_entry_fails()
    {
        var s = await CreateScenarioAsync();

        var response = await s.Manager.Client.PostAsync($"/api/time_entries/{Guid.NewGuid()}/accept", null);

        Assert.False(response.IsSuccessStatusCode);
    }
}
