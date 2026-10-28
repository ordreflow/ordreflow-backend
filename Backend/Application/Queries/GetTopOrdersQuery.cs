namespace Application.Queries;

public sealed record GetTopOrdersQuery(
    DateTime FromDate,
    DateTime ToDate,
    int Limit = 10,
    bool OnlyAccepted = true);