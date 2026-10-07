namespace Application.Commands;

public sealed record UpdateTimeEntryCommand(
    Guid TimeEntryId,
    Guid EmployeeId,
    Guid TaskId,
    DateTime Date,
    decimal Hours,
    string? Comment);
