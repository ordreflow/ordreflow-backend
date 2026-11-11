using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Entities;
using Domain.ValueObjects;
using TaskStatus = Domain.ValueObjects.TaskStatus;

namespace Domain.Services;

public sealed class TimeRegistrationDomainService
{
    public Result Register(
        UserId employeeId,
        UserStatus employeeStatus,
        DepartmentId? employeeDepartmentId,
        DepartmentId departmentId,
        DepartmentStatus departmentStatus,
        OrderId orderId,
        bool orderVisibleInDepartment,
        TaskId taskId,
        TaskStatus taskStatus,
        bool taskAssignmentOnly,
        bool hasActiveAssignment,
        TimeEntryId timeEntryId,
        UserId entryEmployeeId,
        TaskId entryTaskId,
        DateOnly entryWorkDate,
        TimeEntryStatus entryStatus,
        TimeEntryPeriodPolicy periodPolicy,
        DateTimeOffset now)
    {
        if (employeeId is null)
            return Result.Failure(new Error("EmployeeRequired", "An employee is required."));

        if (departmentId is null || departmentStatus != DepartmentStatus.Active)
            return Result.Failure(new Error("DepartmentUnavailable", "An active department is required."));

        if (orderId is null)
            return Result.Failure(new Error("OrderRequired", "An order is required."));

        if (taskId is null)
            return Result.Failure(new Error("TaskRequired", "A task is required."));

        if (timeEntryId is null)
            return Result.Failure(new Error("TimeEntryRequired", "A time entry is required."));

        if (periodPolicy is null)
            return Result.Failure(new Error("PeriodPolicyRequired", "A time entry period policy is required."));

        if (employeeStatus != UserStatus.Active)
            return Result.Failure(new Error("UserInactive", "An inactive user cannot register time."));

        if (employeeDepartmentId != departmentId)
            return Result.Failure(new Error("DepartmentScopeForbidden", "The employee does not belong to this department."));

        if (!orderVisibleInDepartment)
            return Result.Failure(new Error("OrderAccessForbidden", "The order is not visible in the employee's department."));

        if (entryEmployeeId != employeeId)
            return Result.Failure(new Error("EntryOwnerMismatch", "The employee does not own this time entry."));

        if (entryTaskId != taskId)
            return Result.Failure(new Error("TaskMismatch", "The time entry must reference the selected task."));

        if (entryStatus != TimeEntryStatus.Draft)
            return Result.Failure(new Error("TimeEntryNotDraft", "Only a draft time entry can be registered."));

        if (taskStatus is TaskStatus.Finished or TaskStatus.Cancelled)
            return Result.Failure(new Error("TaskInactive", "Time cannot be registered against an inactive task."));

        if (taskAssignmentOnly && !hasActiveAssignment)
            return Result.Failure(new Error("TaskAssignmentRequired", "The employee must have an active assignment to register time against this task."));

        if (!periodPolicy.AllowsWorkDate(entryWorkDate, now))
            return Result.Failure(new Error("WorkDateOutsidePeriod", "The work date is outside the permitted time entry period."));

        return Result.Success();
    }
}
