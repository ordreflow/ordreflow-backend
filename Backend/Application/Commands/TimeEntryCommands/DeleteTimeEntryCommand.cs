namespace Application.Commands;

public sealed record DeleteTimeEntryCommand(
    Guid TimeEntryId,
    Guid EmployeeId);
