using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.ValueObjects;
using WorkTask = Domain.Entities.Task;

namespace Domain.Aggregate;

public sealed class Department
{
    private readonly List<Catagory> _catagories = new();
    public DepartmentId Id { get; private set; } = null!;
    public DepartmentName Name { get; private set; } = null!;
    public IReadOnlyCollection<Catagory> Catagories => _catagories.AsReadOnly();
    private Department() { }

    public static Result<Department> Create(DepartmentName name) => name is null
        ? Result<Department>.Failure(new Error("DepartmentNameRequired", "A department name is required."))
        : Result<Department>.Success(new Department { Id = DepartmentId.Create(Guid.NewGuid()).Value, Name = name });

    public Result<Catagory> AddCatagory(EmployeeId creator, CatagoryName name, bool internalOnly, DateTime createdAtUtc, string? description = null)
    {
        var result = Catagory.Create(Id, creator, name, internalOnly, createdAtUtc, description);
        if (result.IsSuccess) _catagories.Add(result.Value);
        return result;
    }

    public Result<WorkTask> AddTask(CatagoryId catagoryId, TaskName name, DateTime createdAtUtc, string? description = null,
        WorkItemKind kind = WorkItemKind.Task, bool assignmentOnly = false)
    {
        var catagory = _catagories.SingleOrDefault(item => item.Id == catagoryId);
        return catagory is null
            ? Result<WorkTask>.Failure(new Error("CatagoryNotFound", "The category was not found in this department."))
            : catagory.AddTask(name, createdAtUtc, description, kind, assignmentOnly);
    }

    public Result RenameCatagory(CatagoryId id, CatagoryName name)
    {
        var category = _catagories.SingleOrDefault(item => item.Id == id);
        return category is null ? MissingCategory() : category.Rename(name);
    }

    public Result UpdateCatagory(CatagoryId id, CatagoryName name, string? description)
    {
        var category = _catagories.SingleOrDefault(item => item.Id == id);
        return category is null ? MissingCategory() : category.Update(name, description);
    }

    public Result UpdateTask(TaskId taskId, TaskName name, string? description, WorkItemKind kind, bool assignmentOnly)
    {
        var category = taskId is null ? null : FindCatagoryForTask(taskId);
        return category is null
            ? Result.Failure(new Error("TaskNotFound", "The task was not found in this department."))
            : category.UpdateTask(taskId!, name, description, kind, assignmentOnly);
    }

    public Result CloseCatagory(CatagoryId id, DateTime atUtc)
    {
        var category = _catagories.SingleOrDefault(item => item.Id == id);
        return category is null ? MissingCategory() : category.Close(atUtc);
    }

    public Result ReopenCatagory(CatagoryId id)
    {
        var category = _catagories.SingleOrDefault(item => item.Id == id);
        if (category is null) return MissingCategory();
        category.Reopen();
        return Result.Success();
    }

    public Result CloseTask(CatagoryId categoryId, TaskId taskId, DateTime atUtc)
    {
        var category = _catagories.SingleOrDefault(item => item.Id == categoryId);
        return category is null ? MissingCategory() : category.CloseTask(taskId, atUtc);
    }

    public Result ReopenTask(CatagoryId categoryId, TaskId taskId)
    {
        var category = _catagories.SingleOrDefault(item => item.Id == categoryId);
        return category is null ? MissingCategory() : category.ReopenTask(taskId);
    }

    public Catagory? FindCatagoryForTask(TaskId taskId) =>
        _catagories.SingleOrDefault(category => category.Tasks.Any(task => task.Id == taskId));

    public Result CanRegisterTime(TaskId taskId)
    {
        if (taskId is null) return Result.Failure(new Error("TaskRequired", "A task ID is required."));
        var category = FindCatagoryForTask(taskId);
        return category is null
            ? Result.Failure(new Error("TaskNotFound", "The task was not found in this department."))
            : category.CanRegisterTime(taskId);
    }

    private static Result MissingCategory() => Result.Failure(new Error("CatagoryNotFound", "The category was not found in this department."));
}
