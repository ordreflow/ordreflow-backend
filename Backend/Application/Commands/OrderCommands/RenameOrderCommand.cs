using Domain.ValueObjects;

namespace Application.Commands;


public sealed record RenameOrderCommand(
    Guid OrderId,
    Guid ActorId,
    string Name);