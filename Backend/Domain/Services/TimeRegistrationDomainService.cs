using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.ValueObjects;

namespace Domain.Services;

public sealed class TimeRegistrationDomainService
{
    public Result Register(
        User employee,
        Order order,
        TimeEntry entry)
    {
        if (employee is null)
            return Result.Failure(
                new Error(
                    "EmployeeRequired",
                    "An employee is required."));

        if (employee.Status != UserStatus.Active)
            return Result.Failure(
                new Error(
                    "UserInactive",
                    "An inactive user cannot register time."));

        if (order is null)
            return Result.Failure(
                new Error(
                    "OrderRequired",
                    "An order is required."));

        if (entry is null)
            return Result.Failure(
                new Error(
                    "TimeEntryRequired",
                    "A time entry is required."));

        if (entry.EmployeeId != employee.UserId)
            return Result.Failure(
                new Error(
                    "EntryOwnerMismatch",
                    "The employee does not own this time entry."));

        var orderValidation = order.CanRegisterTime(
            entry.TaskId,
            employee.UserId,
            employee.ManagerId);

        if (orderValidation.IsFailure)
            return orderValidation;

        return Result.Success();
    }
}