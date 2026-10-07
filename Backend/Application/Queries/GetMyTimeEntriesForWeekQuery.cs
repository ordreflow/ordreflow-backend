using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Queries;

public sealed record GetMyTimeEntriesForWeekQuery(
    UserId EmployeeId,
    DateTime WeekStart,
    DateTime WeekEndExclusive);
