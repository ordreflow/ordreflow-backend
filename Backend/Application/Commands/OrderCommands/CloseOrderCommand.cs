namespace Application.Commands;

public sealed record CloseOrderCommand(
    Guid OrderId,
    Guid ActorId);