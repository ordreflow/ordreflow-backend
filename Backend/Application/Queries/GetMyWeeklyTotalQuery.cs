using Domain.ValueObjects;

namespace Application.Queries;

public sealed record GetMyWeeklyTotalQuery(
    UserId EmployeeId,
    DateTime WeekStart,
    DateTime WeekEndExclusive);
