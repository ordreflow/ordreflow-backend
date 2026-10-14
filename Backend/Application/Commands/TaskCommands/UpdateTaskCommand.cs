namespace Application.Commands;

public sealed record UpdateTaskCommand(
    Guid ActorId,
    Guid TaskId,
    string Title,
    string Description);
