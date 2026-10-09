namespace Application.Queries;

public sealed record GetOrderQuery(Guid ActorId, Guid OrderId);
