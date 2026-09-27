namespace WebAPI.Contracts.TimeEntries;

public sealed record UpdateTimeEntryRequest(
    int WorkItemId,
    DateTime Date,
    decimal Hours,
    string? Comment);
