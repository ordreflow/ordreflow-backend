namespace Application.Commands;

public sealed record CreateTimeEntryCommand(
	Guid EmployeeId,
	Guid TaskId,
	DateTime Date,
	decimal Hours,
	string? Comment);