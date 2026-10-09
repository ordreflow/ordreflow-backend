namespace Application.Queries;

public sealed record GetTaskQuery(Guid ActorId, Guid TaskId);
