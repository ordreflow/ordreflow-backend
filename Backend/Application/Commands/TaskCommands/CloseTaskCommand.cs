namespace Application.Commands;

public sealed record CloseTaskCommand(
    Guid TaskId,
    Guid ActorId);
