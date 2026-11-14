using Application.Commands;
using Application.Interfaces;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.Aggregate;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class CreateTimeEntryHandler(
    ITimeEntryRepository timeEntryRepository,
    IRegistrationPeriodModeRepository registrationPeriodModeRepository,
    IWorkItemEligibilityQuery workItemEligibilityQuery) : ICommandHandler<CreateTimeEntryCommand>
{
    public async Task<Result> HandleAsync(CreateTimeEntryCommand command)
    {
        var employeeId = EmployeeId.Create(command.EmployeeId);
        var taskId = TaskId.Create(command.TaskId);

        if (employeeId.IsFailure)
            return Result.Failure(employeeId.Errors.ToArray());
        if (taskId.IsFailure)
            return Result.Failure(taskId.Errors.ToArray());

        var workDate = TimeEntryHandlerSupport.ToWorkDate(command.Date);
        var duration = WorkDuration.Create((double)command.Hours);
        var description = WorkDescription.Create(command.Comment);
        var validations = Result.Combine(workDate, duration, description);
        if (validations.IsFailure)
            return validations;

        var permittedWorkItem = await TimeEntryHandlerSupport.GetPermittedWorkItemAsync(
            workItemEligibilityQuery,
            employeeId.Value,
            taskId.Value);
        if (permittedWorkItem.IsFailure)
            return Result.Failure(permittedWorkItem.Errors.ToArray());

        var permittedDates = await TimeEntryHandlerSupport.GetPermittedWorkDatesAsync(
            registrationPeriodModeRepository,
            DateOnly.FromDateTime(DateTime.UtcNow));
        if (permittedDates.IsFailure)
            return Result.Failure(permittedDates.Errors.ToArray());

        var recordedAtUtc = DateTime.UtcNow;
        var entryResult = TimeEntry.Record(
            employeeId.Value,
            taskId.Value,
            workDate.Value,
            duration.Value,
            description.Value,
            permittedDates.Value,
            recordedAtUtc);
        if (entryResult.IsFailure)
            return Result.Failure(entryResult.Errors.ToArray());

        var submitted = entryResult.Value.Submit(employeeId.Value, recordedAtUtc.AddMilliseconds(1));
        if (submitted.IsFailure)
            return submitted;

        await timeEntryRepository.AddAsync(entryResult.Value);
        return Result.Success();
    }
}