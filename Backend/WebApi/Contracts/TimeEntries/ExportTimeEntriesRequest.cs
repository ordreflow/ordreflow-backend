namespace WebAPI.Contracts.TimeEntries;

public sealed record ExportTimeEntriesRequest(
    DateOnly FromDate,
    DateOnly ToDate);
