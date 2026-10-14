namespace Application.Queries;

public sealed record GetMyWeeklyTotalQuery(
    Guid EmployeeId,
    DateTime WeekStart,
    DateTime WeekEndExclusive);
