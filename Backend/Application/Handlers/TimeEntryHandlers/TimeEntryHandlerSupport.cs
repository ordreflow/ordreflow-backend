using Application.Interfaces;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using TaskStatus = Domain.ValueObjects.TaskStatus;

namespace Application.Handlers;

internal static class TimeEntryHandlerSupport
{
    internal static async Task<Result<WorkDatePeriod>> GetPermittedWorkDatesAsync(
        IRegistrationPeriodModeRepository repository,
        DateOnly today)
    {
        var mode = await repository.GetCurrentAsync();
        return mode is null
            ? Result<WorkDatePeriod>.Failure(
                new Error("RegistrationPeriodNotConfigured", "The registration period has not been configured."))
            : mode.GetPermittedWorkDates(today);
    }

    internal static async Task<Result<WorkItemEligibility>> GetPermittedWorkItemAsync(
        IWorkItemEligibilityQuery query,
        EmployeeId employeeId,
        TaskId taskId)
    {
        var eligibility = await query.GetAsync(employeeId, taskId);
        if (eligibility is null)
            return Result<WorkItemEligibility>.Failure(
                new Error("WorkItemNotFound", "The task was not found."));
        if (!eligibility.IsEmployeeActive)
            return Result<WorkItemEligibility>.Failure(
                new Error("EmployeeInactive", "Only active employees can register time."));
        if (!Enum.IsDefined(eligibility.TaskStatus) || !Enum.IsDefined(eligibility.CatagoryStatus))
            return Result<WorkItemEligibility>.Failure(
                new Error("InvalidWorkItemStatus", "The task or category status is invalid."));
        if (eligibility.TaskStatus != TaskStatus.Active ||
            eligibility.CatagoryStatus != CatagoryStatus.Active)
            return Result<WorkItemEligibility>.Failure(
                new Error("WorkItemUnavailable", "The task and category must be active."));
        if (!eligibility.IsAssignedToEmployee)
            return Result<WorkItemEligibility>.Failure(
                new Error("WorkItemNotAssigned", "The task must be assigned to the employee."));
        if (eligibility.IsInternalOnly && !eligibility.IsEmployeeInDepartment)
            return Result<WorkItemEligibility>.Failure(
                new Error("WorkItemAccessForbidden", "The task is restricted to employees in its department."));

        return Result<WorkItemEligibility>.Success(eligibility);
    }

    internal static Result<WorkDate> ToWorkDate(DateTime value) =>
        value == default
            ? Result<WorkDate>.Failure(new Error("WorkDateRequired", "A work date is required."))
            : WorkDate.Create(DateOnly.FromDateTime(value));
}
