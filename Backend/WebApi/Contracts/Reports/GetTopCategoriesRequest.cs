namespace WebAPI.Contracts.Reports;

public sealed record GetTopCategoriesRequest(DateOnly FromDate, DateOnly ToDate);
