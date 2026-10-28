namespace WebAPI.Contracts.Reports;


/// <summary>An order and its total registered hours for the selected date range.</summary>
public sealed record TopOrdersResponse(
    Guid OrderId,
    string OrderName,
    decimal TotalHours);