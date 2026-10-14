namespace Application.Commands;

public sealed record ReturnTimeEntryCommand(
    Guid TimeEntryId,
    Guid ReviewerId,
    string Reason);
