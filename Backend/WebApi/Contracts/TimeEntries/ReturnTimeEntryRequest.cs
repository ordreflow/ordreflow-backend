namespace WebAPI.Contracts.TimeEntries;

// Kept for clients using the original /return route; its JSON shape matches RejectTimeEntryRequest.
public sealed record ReturnTimeEntryRequest(string Reason);
