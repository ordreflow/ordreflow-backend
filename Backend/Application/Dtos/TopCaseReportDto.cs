namespace Application.Dtos;

public sealed record TopOrdersReportDto(
    Guid OrderId,
    string OrderName,
    decimal TotalHours);