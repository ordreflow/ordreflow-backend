namespace Application.Commands;

public sealed record UpdateUserCommand(
    Guid ActorId,
    Guid TargetUserId,
    string Name,
    string Email);
