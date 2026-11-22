using Core.Tools.OperationResult;
using Domain.ValueObjects;
using TaskStatus = Domain.ValueObjects.TaskStatus;

namespace Domain.Entities;

public sealed class Catagory
{
    private readonly List<Task> _tasks = new();
    public CatagoryId Id { get; private set; } = null!;
    public EmployeeId CreatedBy { get; private set; } = null!;
    public DepartmentId DepartmentId { get; private set; } = null!;
    public CatagoryName Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public CatagoryStatus Status { get; private set; }
    public bool InternalOnly { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public IReadOnlyCollection<Task> Tasks => _tasks.AsReadOnly();
    private Catagory() { }

    internal static Result<Catagory> Create(DepartmentId department, EmployeeId creator, CatagoryName name,
        bool internalOnly, DateTime createdAtUtc, string? description)
    {
        if (department is null || creator is null || name is null)
            return Result<Catagory>.Failure(new Error("CatagoryDetailsRequired", "Department, creator, and name are required."));
        if (createdAtUtc == default || createdAtUtc.Kind != DateTimeKind.Utc)
            return Result<Catagory>.Failure(new Error("CreatedAtUtcRequired", "A UTC creation time is required."));
        return Result<Catagory>.Success(new Catagory
        {
            Id = CatagoryId.Create(Guid.NewGuid()).Value, DepartmentId = department, CreatedBy = creator,
            Name = name, InternalOnly = internalOnly, CreatedAt = createdAtUtc, Description = description, Status = CatagoryStatus.Active
        });
    }

    internal Result<Task> AddTask(TaskName name, DateTime atUtc, string? description,
        WorkItemKind kind = WorkItemKind.Task, bool assignmentOnly = false)
    {
        if (Status != CatagoryStatus.Active)
            return Result<Task>.Failure(new Error("CatagoryClosed", "Work cannot be added to a closed category."));
        var result = Task.Create(Id, name, atUtc, description, kind, assignmentOnly);
        if (result.IsSuccess) _tasks.Add(result.Value);
        return result;
    }

    internal Result Rename(CatagoryName name)
    {
        if (name is null) return Result.Failure(new Error("CatagoryNameRequired", "A category name is required."));
        Name = name;
        return Result.Success();
    }

    internal Result Update(CatagoryName name, string? description)
    {
        var renamed = Rename(name);
        if (renamed.IsFailure) return renamed;
        Description = description;
        return Result.Success();
    }

    internal Result UpdateTask(TaskId id, TaskName name, string? description, WorkItemKind kind, bool assignmentOnly)
    {
        var task = _tasks.SingleOrDefault(item => item.Id == id);
        return task is null
            ? Result.Failure(new Error("TaskNotFound", "The task was not found in this category."))
            : task.Update(name, description, kind, assignmentOnly);
    }

    internal Result Close(DateTime atUtc)
    {
        if (Status == CatagoryStatus.Closed) return Result.Success();
        if (atUtc.Kind != DateTimeKind.Utc || atUtc < CreatedAt)
            return Result.Failure(new Error("ClosureTimeInvalid", "The closing time must be UTC and not precede creation."));
        Status = CatagoryStatus.Closed;
        ClosedAt = atUtc;
        return Result.Success();
    }

    internal void Reopen() { Status = CatagoryStatus.Active; ClosedAt = null; }

    internal Result CloseTask(TaskId id, DateTime atUtc)
    {
        var task = _tasks.SingleOrDefault(item => item.Id == id);
        return task is null ? Result.Failure(new Error("TaskNotFound", "The task was not found in this category.")) : task.Close(atUtc);
    }

    internal Result ReopenTask(TaskId id)
    {
        var task = _tasks.SingleOrDefault(item => item.Id == id);
        if (task is null) return Result.Failure(new Error("TaskNotFound", "The task was not found in this category."));
        task.Reopen();
        return Result.Success();
    }

    internal Result CanRegisterTime(TaskId id)
    {
        var task = _tasks.SingleOrDefault(item => item.Id == id);
        if (task is null) return Result.Failure(new Error("TaskNotFound", "The task was not found in this category."));
        if (Status != CatagoryStatus.Active) return Result.Failure(new Error("CatagoryClosed", "Time cannot be registered in a closed category."));
        return task.Status != TaskStatus.Active
            ? Result.Failure(new Error("TaskClosed", "Time cannot be registered on a closed task.")) : Result.Success();
    }
}
