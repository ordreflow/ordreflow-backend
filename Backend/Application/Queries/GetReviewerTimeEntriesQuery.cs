using Domain.ValueObjects;

namespace Application.Queries;

public sealed record GetReviewerTimeEntriesQuery(
    Guid ReviewerId,
    DateTime? FromDate,
    DateTime? ToDateExclusive,
    TimeEntryStatus? Status);
