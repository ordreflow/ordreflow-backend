namespace WebAPI.Contracts.Reports;

public sealed record TopOrdersResponse(
    Guid OrderId,
    string OrderName,
    decimal TotalHours,
    IReadOnlyList<TopOrderTaskResponse> Tasks);

public sealed record TopOrderTaskResponse(
    Guid TaskId,
    string TaskName,
    decimal TotalHours);