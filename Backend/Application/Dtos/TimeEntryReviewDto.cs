namespace Application.Dtos;

public sealed record TimeEntryReviewDto(
    Guid Id,
    Guid TimeEntryId,
    string Decision,
    string? Reason,
    DateTime ReviewedAt);
