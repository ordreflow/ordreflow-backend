namespace Application.Commands;

public sealed record ResubmitTimeEntryCommand(
    Guid TimeEntryId,
    Guid EmployeeId);
