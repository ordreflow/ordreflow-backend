namespace Application.Commands;

public sealed record DeactivateUserCommand(
    Guid ActorId,
    Guid TargetUserId);
