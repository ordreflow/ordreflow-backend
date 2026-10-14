namespace Application.Commands;

public sealed record CreateUserCommand(
    Guid ActorId,
    string Name,
    string Email,
    string Role);
