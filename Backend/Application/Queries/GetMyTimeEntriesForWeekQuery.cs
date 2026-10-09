namespace Application.Queries;

public sealed record GetMyTimeEntriesForWeekQuery(
    Guid EmployeeId,
    DateTime WeekStart,
    DateTime WeekEndExclusive);
