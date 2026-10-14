using Domain.ValueObjects;

namespace Application.Queries;

public sealed record GetManagerTimeEntriesQuery(
    Guid ManagerId,
    DateTime? FromDate,
    DateTime? ToDateExclusive,
    TimeEntryStatus? Status);
