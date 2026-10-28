namespace WebAPI.Contracts.Reports;

/// <summary>Top orders ranked by registered hours.</summary>
public sealed record GetTopOrdersResponse(
    IReadOnlyList<TopOrdersResponse> Items);