namespace WebAPI.Contracts.TimeEntries;

public sealed record ViewTimeEntriesRequest(
    DateOnly FromDate,
    DateOnly ToDate);
