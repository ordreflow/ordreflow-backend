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
    public async Task Employee_correction_moves_returned_entry_directly_to_draft()
    {
        var (s, entry) = await ReturnedEntryAsync();

        var response = await s.Employee.Client.PutAsJsonAsync($"/api/time_entries/{entry.Id}",
            new UpdateTimeEntryRequest(Monday, 6m, "corrected"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = await GetEntryAsync(s.Employee, entry.Id);
        Assert.Equal(6m, updated.Hours);
        Assert.Equal("corrected", updated.Comment);
        Assert.Equal("Draft", updated.Status);
    }

    [Fact]
    public async Task Employee_can_resubmit_unedited_returned_entry_to_draft()
    {
        var (s, entry) = await ReturnedEntryAsync();

        var response = await s.Employee.Client.PostAsync($"/api/time_entries/{entry.Id}/resubmit", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var resubmitted = await GetEntryAsync(s.Employee, entry.Id);
        Assert.Equal("Draft", resubmitted.Status);
        Assert.Equal(4m, resubmitted.Hours);
        Assert.Equal("first try", resubmitted.Comment);
    }

    [Fact]
    public async Task Unedited_resubmitted_entry_can_be_accepted_by_manager()
    {
        var (s, entry) = await ReturnedEntryAsync();
        await s.Employee.Client.PostAsync($"/api/time_entries/{entry.Id}/resubmit", null);

        var accept = await s.Manager.Client.PostAsync($"/api/time_entries/{entry.Id}/accept", null);

        Assert.Equal(HttpStatusCode.NoContent, accept.StatusCode);
        Assert.Equal("Accepted", (await GetEntryAsync(s.Employee, entry.Id)).Status);
    }

    [Fact]
    public async Task Corrected_returned_entry_can_be_accepted_without_resubmit_and_keeps_review_history()
    {
        var (s, entry) = await ReturnedEntryAsync();
        var originalReviews = await s.Employee.Client.GetFromJsonAsync<GetTimeEntryReviewsResponse>(
            $"/api/time_entries/{entry.Id}/reviews");
        var returnedReview = Assert.Single(originalReviews!.Items);
        Assert.Equal("Returned", returnedReview.Decision);
        Assert.Equal("Wrong hours", returnedReview.Reason);

        var correctedDate = Monday.AddDays(1);
        var correction = await s.Employee.Client.PutAsJsonAsync($"/api/time_entries/{entry.Id}",
            new UpdateTimeEntryRequest(correctedDate, 6m, "corrected"));

        Assert.Equal(HttpStatusCode.NoContent, correction.StatusCode);
        var corrected = await GetEntryAsync(s.Employee, entry.Id);
        Assert.Equal(correctedDate.Date, corrected.Date.Date);
        Assert.Equal(6m, corrected.Hours);
        Assert.Equal("corrected", corrected.Comment);
        Assert.Equal("Draft", corrected.Status);
        var reviewsAfterCorrection = await s.Employee.Client.GetFromJsonAsync<GetTimeEntryReviewsResponse>(
            $"/api/time_entries/{entry.Id}/reviews");
        Assert.Equal(returnedReview, Assert.Single(reviewsAfterCorrection!.Items));

        var accept = await s.Manager.Client.PostAsync($"/api/time_entries/{entry.Id}/accept", null);

        Assert.Equal(HttpStatusCode.NoContent, accept.StatusCode);
        Assert.Equal("Accepted", (await GetEntryAsync(s.Employee, entry.Id)).Status);
        var reviewsAfterAccept = await s.Manager.Client.GetFromJsonAsync<GetTimeEntryReviewsResponse>(
            $"/api/time_entries/{entry.Id}/reviews");
        Assert.Equal(2, reviewsAfterAccept!.Items.Count);
        Assert.Contains(returnedReview, reviewsAfterAccept.Items);
        var acceptedReview = Assert.Single(reviewsAfterAccept.Items.Where(r => r.Decision == "Accepted"));
        Assert.Null(acceptedReview.Reason);
    }

    [Theory]
    [InlineData("date")]
    [InlineData("comment")]
    public async Task Invalid_correction_keeps_returned_entry_and_its_review_unchanged(string invalidField)
    {
        var (s, entry) = await ReturnedEntryAsync();
        var before = await GetEntryAsync(s.Employee, entry.Id);
        var originalReviews = await s.Employee.Client.GetFromJsonAsync<GetTimeEntryReviewsResponse>(
            $"/api/time_entries/{entry.Id}/reviews");
        Assert.Equal("Returned", before.Status);
        Assert.Single(originalReviews!.Items);

        var date = invalidField == "date" ? default : Monday.AddDays(1);
        var comment = invalidField == "comment" ? new string('x', 1001) : "corrected";
        var response = await s.Employee.Client.PutAsJsonAsync($"/api/time_entries/{entry.Id}",
            new UpdateTimeEntryRequest(date, 6m, comment));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await GetEntryAsync(s.Employee, entry.Id));
        var reviewsAfter = await s.Employee.Client.GetFromJsonAsync<GetTimeEntryReviewsResponse>(
            $"/api/time_entries/{entry.Id}/reviews");
        Assert.Equal<TimeEntryReviewResponse>(originalReviews.Items, reviewsAfter!.Items);
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
