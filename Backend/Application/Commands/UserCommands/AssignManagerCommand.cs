namespace Application.Commands;

public sealed record AssignManagerCommand(
    Guid AdminId,
    Guid EmployeeId,
    Guid ManagerId);
