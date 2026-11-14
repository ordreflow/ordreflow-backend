using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class CreateTimeEntryHandler(
    IEmployeeAccessRepository employees,
    ITimeRegistrationRepository registration,
    IRegistrationPeriodModeRepository periods,
    TimeProvider clock,
    TimeZoneInfo companyTimeZone) : ICommandHandler<CreateTimeEntryCommand>
{
    public async Task<Result> HandleAsync(CreateTimeEntryCommand command)
    {
        if (command.EmployeeId == Guid.Empty)
            return Result.Failure(new Error("EmployeeRequired", "An employee is required."));

        var employee = await employees.GetWithAccessesAsync(EmployeeId.Create(command.EmployeeId).Value);
        if (employee is null || !employee.HasPermission(SystemPermission.Employee))
            return Result.Failure(new Error("EmployeeAccessForbidden", "Active employee permission is required."));

        if (command.TaskId == Guid.Empty)
            return Result.Failure(new Error("TaskRequired", "A task is required."));
        if (command.Date == default)
            return Result.Failure(new Error("WorkDateRequired", "A work date is required."));
        var duration = Duration.Create(command.Hours);
        if (duration.IsFailure)
            return Result.Failure(duration.Errors.ToArray());
        var workDate = WorkDate.Create(DateOnly.FromDateTime(command.Date));
        if (workDate.IsFailure)
            return Result.Failure(workDate.Errors.ToArray());
        var description = TimeEntryDescription.Create(command.Comment);
        if (description.IsFailure)
            return Result.Failure(description.Errors.ToArray());

        var task = await registration.GetTaskAsync(TaskId.Create(command.TaskId).Value);
        if (task is null)
            return Result.Failure(new Error("TaskNotFound", "The task was not found."));

        var catagory = await registration.GetCatagoryAsync(task.CatagoryId);
        if (catagory is null)
            return Result.Failure(new Error("CatagoryNotFound", "The task's category was not found."));

        var isDepartmentMember = !catagory.InternalOnly ||
            await registration.IsDepartmentMemberAsync(employee.Id, catagory.DepartmentId);
        var assignment = await registration.GetAssignmentAccessAsync(task.Id, employee.Id);
        var access = catagory.CanRegisterTime(
            task, employee, isDepartmentMember, assignment.HasAssignments, assignment.IsAssigned);
        if (access.IsFailure)
            return access;

        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, companyTimeZone).DateTime);
        var permittedDates = await TimeEntryHandlerSupport.GetPermittedWorkDatesAsync(periods, today);
        if (permittedDates.IsFailure)
            return Result.Failure(permittedDates.Errors.ToArray());

        var entry = TimeEntry.Record(
            employee.Id, task.Id, workDate.Value, duration.Value,
            description.Value, permittedDates.Value, now.UtcDateTime);
        if (entry.IsFailure)
            return Result.Failure(entry.Errors.ToArray());

        await registration.AddAsync(entry.Value);
        return Result.Success();
    }
}
