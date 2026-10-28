namespace WebAPI.Contracts.Reports;


/// <summary>Inclusive calendar-date range for the top orders report.</summary>
public sealed record GetTopOrdersRequest(
    DateOnly FromDate,
    DateOnly ToDate);