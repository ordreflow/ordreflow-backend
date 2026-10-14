namespace WebAPI.Contracts.TimeEntries;

public sealed record UpdateTimeEntryRequest(
    DateTime Date,
    decimal Hours,
    string? Comment);
