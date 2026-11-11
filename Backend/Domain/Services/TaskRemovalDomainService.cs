using Core.Tools.OperationResult;
using Domain.ValueObjects;
using WorkTask = Domain.Entities.Task;

namespace Domain.Services;

/// <summary>
/// Cancels a task instead of deleting it so registered time remains historical.
/// </summary>
public sealed class TaskRemovalDomainService
{
    public Result RemoveTask(
        UserId actorId,
        UserRole actorRole,
        UserStatus actorStatus,
        DepartmentId? actorDepartmentId,
        DepartmentId departmentId,
        DepartmentStatus departmentStatus,
        OrderId orderId,
        bool orderVisibleInDepartment,
        WorkTask task,
        bool taskHasTimeEntries)
    {
        if (actorId is null || actorStatus != UserStatus.Active ||
            actorRole is not (UserRole.Manager or UserRole.Admin))
            return Result.Failure(new Error("TaskRemovalForbidden", "Only an active manager or admin can remove tasks."));

        if (departmentId is null || departmentStatus != DepartmentStatus.Active ||
            orderId is null || task is null ||
            !orderVisibleInDepartment ||
            task.OrderId != orderId)
            return Result.Failure(new Error("TaskNotFound", "The task was not found in the selected department and order."));

        if (actorRole == UserRole.Manager && actorDepartmentId != departmentId)
            return Result.Failure(new Error("DepartmentScopeForbidden", "A manager can only remove tasks in their own department."));

        if (taskHasTimeEntries)
            return Result.Failure(new Error("TaskHasTimeEntries", "The task cannot be deleted because time entries reference it."));

        return task.Cancel();
    }
}
