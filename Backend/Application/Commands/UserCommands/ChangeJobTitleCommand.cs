namespace Application.Commands;

public sealed record ChangeJobTitleCommand(
    Guid ActorId,
    Guid UserId,
    string? JobTitle);
