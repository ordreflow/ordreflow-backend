namespace Application.Queries;

public sealed record GetTopCategoriesQuery(Guid ActorId, DateOnly FromDate, DateOnly ToDate);
