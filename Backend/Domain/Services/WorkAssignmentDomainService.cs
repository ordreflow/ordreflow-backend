using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.ValueObjects;
using TaskStatus = Domain.ValueObjects.TaskStatus;

namespace Domain.Services;

public sealed class WorkAssignmentDomainService
{
    public Result<WorkAssignment> Assign(
        UserId actorId,
        UserRole actorRole,
        UserStatus actorStatus,
        DepartmentId? actorDepartmentId,
        UserId assigneeId,
        UserStatus assigneeStatus,
        DepartmentId? assigneeDepartmentId,
        DepartmentId departmentId,
        DepartmentStatus departmentStatus,
        OrderId orderId,
        bool orderVisibleInDepartment,
        TaskId taskId,
        TaskKind taskKind,
        TaskStatus taskStatus,
        int expectedMinutes,
        DateOnly? targetCompletionDate)
    {
        if (actorId is null || assigneeId is null)
            return Result<WorkAssignment>.Failure(new Error("UserRequired", "An actor and assignee are required."));

        if (departmentId is null || departmentStatus != DepartmentStatus.Active)
            return Result<WorkAssignment>.Failure(new Error("DepartmentUnavailable", "An active department is required."));

        if (orderId is null || taskId is null)
            return Result<WorkAssignment>.Failure(new Error("TaskRequired", "An order and task are required."));

        if (!Enum.IsDefined(taskKind) || taskKind != TaskKind.Work)
            return Result<WorkAssignment>.Failure(new Error("GroupTaskCannotBeAssigned", "Assignments can only be added to work tasks."));

        if (taskStatus is TaskStatus.Finished or TaskStatus.Cancelled)
            return Result<WorkAssignment>.Failure(new Error("TaskInactive", "An inactive task cannot receive assignments."));

        if (actorStatus != UserStatus.Active ||
            actorRole is not (UserRole.Manager or UserRole.Admin))
            return Result<WorkAssignment>.Failure(new Error("AssignmentForbidden", "Only an active manager or admin can assign work."));

        if (actorRole == UserRole.Manager && actorDepartmentId != departmentId)
            return Result<WorkAssignment>.Failure(new Error("DepartmentScopeForbidden", "A manager can only assign work in their own department."));

        if (assigneeStatus != UserStatus.Active || assigneeDepartmentId != departmentId)
            return Result<WorkAssignment>.Failure(new Error("AssigneeDepartmentMismatch", "The assignee must be active and belong to the department."));

        if (!orderVisibleInDepartment)
            return Result<WorkAssignment>.Failure(new Error("OrderAccessForbidden", "The order is not visible in the department."));

        return WorkAssignment.Create(taskId, assigneeId, expectedMinutes, targetCompletionDate);
    }
}
