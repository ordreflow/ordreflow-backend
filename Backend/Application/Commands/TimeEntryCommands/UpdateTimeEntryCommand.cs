namespace Application.Commands;

public sealed record UpdateTimeEntryCommand(
    Guid TimeEntryId,
    Guid EmployeeId,
    DateTime Date,
    decimal Hours,
    string? Comment);
