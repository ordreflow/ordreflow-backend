namespace Application.Commands;

public sealed record ApproveTimeEntryCommand(
    Guid TimeEntryId,
    Guid ReviewerId);
