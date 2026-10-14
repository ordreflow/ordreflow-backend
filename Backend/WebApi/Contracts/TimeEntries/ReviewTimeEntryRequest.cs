namespace WebAPI.Contracts.TimeEntries;

public sealed record ReviewTimeEntryRequest(
    string Decision,
    string? Reason);
