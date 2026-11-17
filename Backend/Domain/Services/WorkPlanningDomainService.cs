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

    private static Result Forbidden() => Result.Failure(new Error("WorkPlanningForbidden",
        "Only a manager of the owning department or an administrator can plan work here."));
}
