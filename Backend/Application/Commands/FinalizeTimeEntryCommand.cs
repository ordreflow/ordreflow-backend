namespace Application.Commands;

public sealed record FinalizeTimeEntryCommand(
    Guid TimeEntryId,
    Guid ReviewerId);
