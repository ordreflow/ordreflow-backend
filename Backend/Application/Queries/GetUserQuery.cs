namespace Application.Queries;

public sealed record GetUserQuery(Guid ActorId, Guid TargetUserId);
