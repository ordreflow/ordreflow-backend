namespace Application.Commands;

public sealed record AcceptTimeEntryCommand(
    Guid TimeEntryId,
    Guid ReviewerId);
