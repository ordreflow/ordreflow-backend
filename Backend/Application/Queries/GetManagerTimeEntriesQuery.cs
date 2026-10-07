using Domain.ValueObjects;

namespace Application.Queries;

public sealed record GetManagerTimeEntriesQuery(
    UserId ManagerId,
    DateTime? FromDate,
    DateTime? ToDateExclusive,
    TimeEntryStatus? Status);
