namespace Application.Commands;

public sealed record ReopenTaskCommand(
    Guid TaskId,
    Guid ActorId);
