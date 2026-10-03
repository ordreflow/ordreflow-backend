namespace WebAPI.Contracts.TimeEntries;

public sealed record CreateTimeEntryRequest(
    Guid TaskId,
    DateTime Date,
    decimal Hours,
    string? Comment);
