namespace WebAPI.Contracts.Reports;

public sealed record GetTopCategoriesResponse(IReadOnlyList<TopCategoryResponse> Items);
public sealed record TopCategoryResponse(Guid CategoryId, string CategoryName, Guid DepartmentId,
    string DepartmentName, decimal TotalHours, IReadOnlyList<TopCategoryTaskResponse> Tasks);
public sealed record TopCategoryTaskResponse(Guid TaskId, string TaskName, decimal TotalHours);
