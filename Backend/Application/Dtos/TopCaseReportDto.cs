namespace Application.Dtos;

public sealed record TopOrdersReportDto(
    Guid OrderId,
    string OrderName,
    decimal TotalHours,
    IReadOnlyList<TopOrderTaskDto> Tasks);

public sealed record TopOrderTaskDto(
    Guid TaskId,
    string TaskName,
    decimal TotalHours);