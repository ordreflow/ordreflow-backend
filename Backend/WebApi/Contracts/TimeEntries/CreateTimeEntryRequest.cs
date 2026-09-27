namespace WebAPI.Contracts.TimeEntries;

public sealed record CreateTimeEntryRequest(
    int WorkItemId,
    DateTime Date,
    decimal Hours,
    string? Comment);
