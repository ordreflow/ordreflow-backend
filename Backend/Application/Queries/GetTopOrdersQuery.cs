namespace Application.Queries;

public sealed record GetTopOrdersQuery(
    Guid ActorId,
    DateTime FromDate,
    DateTime ToDate,
    int Limit = 10,
    bool OnlyAccepted = true);