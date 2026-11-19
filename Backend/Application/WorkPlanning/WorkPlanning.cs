using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Entities;
using Domain.ValueObjects;
using WorkTask = Domain.Entities.Task;

namespace Application.WorkPlanning;

public sealed record WorkItemAssignmentDto(Guid EmployeeId, decimal ExpectedHours, DateOnly ExpectedDeadline);

public sealed record WorkItemDto(
    Guid Id,
    Guid CategoryId,
    string Name,
    string? Description,
    string Kind,
    bool AssignmentOnly,
    string Status,
    DateTime CreatedAt,
    IReadOnlyList<WorkItemAssignmentDto>? Assignments = null);

public sealed record PlannableCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    Guid DepartmentId,
    string DepartmentName,
    bool InternalOnly,
    string Status,
    IReadOnlyList<WorkItemDto> WorkItems,
    IReadOnlyList<Guid>? ReviewerIds = null);

internal static class WorkPlanningProjection
{
    public static WorkItemDto WorkItem(WorkTask task, ILookup<TaskId, TaskAssignment>? assignments = null) => new(
        task.Id.Value,
        task.CatagoryId.Value,
        task.Name.Value,
        task.Description,
        task.Kind.ToString(),
        task.AssignmentOnly,
        task.Status.ToString(),
        task.CreatedAt,
        assignments?[task.Id]
            .Select(assignment => new WorkItemAssignmentDto(
                assignment.AssignedTo.Value, assignment.ExpectedDuration.Value, assignment.ExpectedDeadline.Value))
            .ToArray() ?? []);

    public static PlannableCategoryDto Category(Department department, Catagory catagory,
        ILookup<TaskId, TaskAssignment>? assignments = null, ILookup<CatagoryId, EmployeeId>? reviewers = null) => new(
        catagory.Id.Value,
        catagory.Name.Value,
        catagory.Description,
        department.Id.Value,
        department.Name.Value,
        catagory.InternalOnly,
        catagory.Status.ToString(),
        catagory.Tasks
            .OrderBy(task => task.Name.Value, StringComparer.CurrentCultureIgnoreCase)
            .Select(task => WorkItem(task, assignments))
            .ToArray(),
        reviewers?[catagory.Id].Select(reviewer => reviewer.Value).ToArray() ?? []);
}

internal static class WorkPlanningInput
{
    public const int MaxDescriptionLength = 1000;

    public static Result<string?> Description(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<string?>.Success(null);

        value = value.Trim();
        return value.Length > MaxDescriptionLength
            ? Result<string?>.Failure(new Error("DescriptionTooLong", $"The description cannot exceed {MaxDescriptionLength} characters."))
            : Result<string?>.Success(value);
    }

    /// <summary>Missing means an ordinary task; numbers are not accepted as kinds.</summary>
    public static Result<WorkItemKind> Kind(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<WorkItemKind>.Success(WorkItemKind.Task);

        return Enum.TryParse<WorkItemKind>(value, true, out var kind) && Enum.IsDefined(kind) && !int.TryParse(value, out _)
            ? Result<WorkItemKind>.Success(kind)
            : Result<WorkItemKind>.Failure(new Error("InvalidWorkItemKind", "The kind must be Project, Ticket or Task."));
    }
}
