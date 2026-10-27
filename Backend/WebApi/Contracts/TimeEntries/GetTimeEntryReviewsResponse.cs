namespace WebAPI.Contracts.TimeEntries;

public sealed record GetTimeEntryReviewsResponse(
    IReadOnlyCollection<TimeEntryReviewResponse> Items);
