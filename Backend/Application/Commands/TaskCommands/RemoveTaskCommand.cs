namespace Application.Commands;

public sealed record RemoveTaskCommand(
    Guid ActorId,
    Guid TaskId);
