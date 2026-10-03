namespace WebAPI.Contracts.TimeEntries;

public sealed record UpdateTimeEntryRequest(
    Guid TaskId,
    DateTime Date,
    decimal Hours,
    string? Comment);
