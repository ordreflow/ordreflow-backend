using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Entities;
using Domain.ValueObjects;
using WorkTask = Domain.Entities.Task;

namespace Domain.Services;

/// <summary>
/// Who may create categories and work (FR-08). A department manager plans work only in categories owned by
/// departments they manage, and only while they hold the DepartmentManager grant; an administrator may plan
/// anywhere. Work is created inside its category and can never be moved to another one (SEC-08).
/// </summary>
public sealed class WorkPlanningDomainService
{
    public Result CanPlanIn(Employee actor, Department department, IReadOnlyCollection<DepartmentId> managedDepartments)
    {
        if (actor is null || department is null || !actor.HasPermission(SystemPermission.Employee))
            return Forbidden();

        if (actor.HasPermission(SystemPermission.Administrator))
            return Result.Success();

        return DepartmentStaffingDomainService.HasManagerAccess(actor) &&
               managedDepartments is not null && managedDepartments.Contains(department.Id)
            ? Result.Success()
            : Forbidden();
    }

    public Result<Catagory> CreateCatagory(Employee actor, Department department, IReadOnlyCollection<DepartmentId> managedDepartments,
        CatagoryName name, bool internalOnly, string? description, DateTime createdAtUtc)
    {
        var allowed = CanPlanIn(actor, department, managedDepartments);
        return allowed.IsFailure
            ? Result<Catagory>.Failure(allowed.Errors.ToArray())
            : department.AddCatagory(actor.Id, name, internalOnly, createdAtUtc, description);
    }

    public Result<WorkTask> CreateWork(Employee actor, Department department, IReadOnlyCollection<DepartmentId> managedDepartments,
        CatagoryId catagoryId, TaskName name, string? description, WorkItemKind kind, bool assignmentOnly, DateTime createdAtUtc)
    {
        var allowed = CanPlanIn(actor, department, managedDepartments);
        return allowed.IsFailure
            ? Result<WorkTask>.Failure(allowed.Errors.ToArray())
            : department.AddTask(catagoryId, name, createdAtUtc, description, kind, assignmentOnly);
    }

    public Result UpdateCatagory(Employee actor, Department department, IReadOnlyCollection<DepartmentId> managedDepartments,
        CatagoryId catagoryId, CatagoryName name, string? description)
    {
        var allowed = CanPlanIn(actor, department, managedDepartments);
        return allowed.IsFailure ? allowed : department.UpdateCatagory(catagoryId, name, description);
    }

    public Result UpdateWork(Employee actor, Department department, IReadOnlyCollection<DepartmentId> managedDepartments,
        TaskId taskId, TaskName name, string? description, WorkItemKind kind, bool assignmentOnly)
    {
        var allowed = CanPlanIn(actor, department, managedDepartments);
        return allowed.IsFailure ? allowed : department.UpdateTask(taskId, name, description, kind, assignmentOnly);
    }

    /// <summary>
    /// Deactivating a category hides it and all its work from time registration without changing the work's own
    /// status, so reactivating the category restores the work as it was. Existing time entries are kept.
    /// </summary>
    public Result SetCatagoryActive(Employee actor, Department department, IReadOnlyCollection<DepartmentId> managedDepartments,
        CatagoryId catagoryId, bool active, DateTime atUtc)
    {
        var allowed = CanPlanIn(actor, department, managedDepartments);
        if (allowed.IsFailure) return allowed;
        return active ? department.ReopenCatagory(catagoryId) : department.CloseCatagory(catagoryId, atUtc);
    }

    /// <summary>Deactivated work cannot be selected for new time registrations. Existing time entries are kept.</summary>
    public Result SetWorkActive(Employee actor, Department department, IReadOnlyCollection<DepartmentId> managedDepartments,
        TaskId taskId, bool active, DateTime atUtc)
    {
        var allowed = CanPlanIn(actor, department, managedDepartments);
        if (allowed.IsFailure) return allowed;
        var catagory = taskId is null ? null : department.FindCatagoryForTask(taskId);
        if (catagory is null) return Result.Failure(new Error("TaskNotFound", "The task was not found in this department."));
        return active ? department.ReopenTask(catagory.Id, taskId!) : department.CloseTask(catagory.Id, taskId!, atUtc);
    }

    private static Result Forbidden() => Result.Failure(new Error("WorkPlanningForbidden",
        "Only a manager of the owning department or an administrator can plan work here."));
}
