namespace Application.Commands;

public sealed record RejectTimeEntryCommand(
    Guid TimeEntryId,
    Guid ReviewerId,
    string? Reason);
