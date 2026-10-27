namespace WebAPI.Contracts.TimeEntries;

public sealed record TimeEntryReviewResponse(
    Guid Id,
    Guid TimeEntryId,
    string Decision,
    string? Reason,
    DateTime ReviewedAt);
