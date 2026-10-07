namespace WebAPI.Contracts.TimeEntries;

public sealed record WeeklyTotalResponse(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal TotalHours);
