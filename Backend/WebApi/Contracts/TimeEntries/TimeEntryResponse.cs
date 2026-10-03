namespace WebAPI.Contracts.TimeEntries;

public sealed record TimeEntryResponse(
    Guid Id,
    Guid TaskId,
    DateTime Date,
    decimal Hours,
    string? Comment);
