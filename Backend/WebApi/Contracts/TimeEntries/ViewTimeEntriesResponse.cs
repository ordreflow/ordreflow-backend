namespace WebAPI.Contracts.TimeEntries;

public sealed record ViewTimeEntriesResponse(
    IReadOnlyCollection<TimeEntryResponse> Items);
