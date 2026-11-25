using Core.Tools.OperationResult;
using Domain.ValueObjects;
using TaskStatus = Domain.ValueObjects.TaskStatus;

namespace Domain.Entities;

public sealed class Task
{
    public TaskId Id { get; private set; } = null!;
    public CatagoryId CatagoryId { get; private set; } = null!;
    public TaskStatus Status { get; private set; }
    public TaskName Name { get; private set; } = null!;
    public string? Description { get; private set; }

    // Project, ticket or task (FR-08). Descriptive only.
    public WorkItemKind Kind { get; private set; } = WorkItemKind.Task;

    // Assignment-only work can only be selected by its assignees, even before anyone is assigned (FR-08, FR-21).
    public bool AssignmentOnly { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    private Task() { }

    internal static Result<Task> Create(CatagoryId category, TaskName name, DateTime createdAtUtc, string? description,
        WorkItemKind kind = WorkItemKind.Task, bool assignmentOnly = false)
    {
        if (category is null || name is null)
            return Result<Task>.Failure(new Error("TaskDetailsRequired", "A category and task name are required."));
        if (createdAtUtc == default || createdAtUtc.Kind != DateTimeKind.Utc)
            return Result<Task>.Failure(new Error("CreatedAtUtcRequired", "A UTC creation time is required."));
        if (!Enum.IsDefined(kind))
            return Result<Task>.Failure(new Error("InvalidWorkItemKind", "The work item kind is invalid."));
        return Result<Task>.Success(new Task { Id = TaskId.Create(Guid.NewGuid()).Value, CatagoryId = category,
            Name = name, Description = description, CreatedAt = createdAtUtc, Status = TaskStatus.Active,
            Kind = kind, AssignmentOnly = assignmentOnly });
    }

    internal Result Update(TaskName name, string? description, WorkItemKind kind, bool assignmentOnly)
    {
        if (name is null)
            return Result.Failure(new Error("TaskNameRequired", "A task name is required."));
        if (!Enum.IsDefined(kind))
            return Result.Failure(new Error("InvalidWorkItemKind", "The work item kind is invalid."));
        Name = name;
        Description = description;
        Kind = kind;
        AssignmentOnly = assignmentOnly;
        return Result.Success();
    }

    internal Result Close(DateTime atUtc)
    {
        if (Status == TaskStatus.Closed) return Result.Success();
        if (atUtc.Kind != DateTimeKind.Utc || atUtc < CreatedAt)
            return Result.Failure(new Error("ClosureTimeInvalid", "The closing time must be UTC and not precede creation."));
        Status = TaskStatus.Closed;
        ClosedAt = atUtc;
        return Result.Success();
    }

    internal void Reopen() { Status = TaskStatus.Active; ClosedAt = null; }
}
