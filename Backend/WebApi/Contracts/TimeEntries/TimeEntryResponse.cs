namespace WebAPI.Contracts.TimeEntries;

public sealed record TimeEntryResponse(
    int Id,
    int WorkItemId,
    DateTime Date,
    decimal Hours,
    string? Comment);
