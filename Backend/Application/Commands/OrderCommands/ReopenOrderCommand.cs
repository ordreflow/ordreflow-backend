namespace Application.Commands;

public sealed record ReopenOrderCommand(
    Guid OrderId,
    Guid ActorId);