namespace Application.Dtos;

public sealed record TopCategoryReportDto(Guid CategoryId, string CategoryName, Guid DepartmentId,
    string DepartmentName, decimal TotalHours, IReadOnlyList<TopCategoryTaskDto> Tasks);

public sealed record TopCategoryTaskDto(Guid TaskId, string TaskName, decimal TotalHours);
