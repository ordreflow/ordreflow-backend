namespace Application.Commands;

public sealed record ChangeUserRoleCommand(
    Guid ActorId,
    Guid UserId,
    string Role);
