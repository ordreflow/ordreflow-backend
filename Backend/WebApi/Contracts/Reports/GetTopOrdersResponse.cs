namespace WebAPI.Contracts.Reports;

public sealed record GetTopOrdersResponse(
    IReadOnlyList<TopOrdersResponse> Items);