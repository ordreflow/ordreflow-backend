using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.Services;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class CreateTimeEntryHandler(
    IEmployeeAccessRepository employees,
    ITimeRegistrationRepository registration,
    IRegistrationPeriodModeRepository periods,
    TimeProvider clock,
    TimeZoneInfo companyTimeZone,
    TimeRegistrationDomainService registrationService) : ICommandHandler<CreateTimeEntryCommand>
{
    public async Task<Result> HandleAsync(CreateTimeEntryCommand command)
    {
        if (command.EmployeeId == Guid.Empty)
            return Result.Failure(new Error("EmployeeRequired", "An employee is required."));

        var employee = await employees.GetWithAccessesAsync(EmployeeId.Create(command.EmployeeId).Value);
        var employeeAccess = registrationService.ValidateEmployee(employee);
        if (employeeAccess.IsFailure)
            return employeeAccess;
        var activeEmployee = employee!;

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

        var taskId = TaskId.Create(command.TaskId).Value;
        var department = await registration.GetDepartmentForTaskAsync(taskId);
        if (department is null)
            return Result.Failure(new Error("TaskNotFound", "The task was not found."));

        var catagory = department.FindCatagoryForTask(taskId);
        if (catagory is null)
            return Result.Failure(new Error("TaskNotFound", "The task was not found in its department."));

        var isDepartmentMember = !catagory.InternalOnly ||
            await registration.IsDepartmentMemberAsync(activeEmployee.Id, department.Id);
        var assignment = await registration.GetAssignmentAccessAsync(taskId, activeEmployee.Id);

        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, companyTimeZone).DateTime);
        var permittedDates = await TimeEntryHandlerSupport.GetPermittedWorkDatesAsync(periods, today);
        if (permittedDates.IsFailure)
            return Result.Failure(permittedDates.Errors.ToArray());

        var entry = registrationService.Register(
            activeEmployee, department, taskId, workDate.Value, duration.Value, description.Value,
            new TimeRegistrationAccess(isDepartmentMember, assignment.HasAssignments, assignment.IsAssigned),
            permittedDates.Value, now.UtcDateTime);
        if (entry.IsFailure)
            return Result.Failure(entry.Errors.ToArray());

        await registration.AddAsync(entry.Value);
        return Result.Success();
    }
}
