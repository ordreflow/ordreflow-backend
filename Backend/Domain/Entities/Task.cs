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
    public DateTime CreatedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    private Task() { }

    internal static Result<Task> Create(CatagoryId category, TaskName name, DateTime createdAtUtc, string? description)
    {
        if (category is null || name is null)
            return Result<Task>.Failure(new Error("TaskDetailsRequired", "A category and task name are required."));
        if (createdAtUtc == default || createdAtUtc.Kind != DateTimeKind.Utc)
            return Result<Task>.Failure(new Error("CreatedAtUtcRequired", "A UTC creation time is required."));
        return Result<Task>.Success(new Task { Id = TaskId.Create(Guid.NewGuid()).Value, CatagoryId = category,
            Name = name, Description = description, CreatedAt = createdAtUtc, Status = TaskStatus.Active });
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
}
