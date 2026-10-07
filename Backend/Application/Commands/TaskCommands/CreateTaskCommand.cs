namespace Application.Commands;

public sealed record CreateTaskCommand(
    Guid OrderId,
    Guid ManagerId,
    string Title,
    string Description);
