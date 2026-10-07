using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class UpdateTimeEntryHandler(
    ITimeEntryRepository repository) : ICommandHandler<UpdateTimeEntryCommand>
{
    public async Task<Result> HandleAsync(UpdateTimeEntryCommand command)
    {
        var entryId = TimeEntryId.Create(command.TimeEntryId);
        var employeeId = UserId.Create(command.EmployeeId);
        var taskId = TaskId.Create(command.TaskId);
        if (entryId.IsFailure)
            return Result.Failure(entryId.Errors.ToArray());
        if (employeeId.IsFailure)
            return Result.Failure(employeeId.Errors.ToArray());
        if (taskId.IsFailure)
            return Result.Failure(taskId.Errors.ToArray());

        var entry = await repository.GetAsync(entryId.Value);
        if (entry is null)
            return Result.Failure(new Error("TimeEntryNotFound", "The time entry was not found."));

        if (entry.TaskId != taskId.Value)
            return Result.Failure(new Error("TaskChangeNotSupported", "The task cannot be changed after creation."));

        var hoursResult = entry.ChangeHours(employeeId.Value, command.Hours);
        if (hoursResult.IsFailure)
            return hoursResult;

        var dateResult = entry.ChangeDate(employeeId.Value, command.Date);
        if (dateResult.IsFailure)
            return dateResult;

        return entry.ChangeComment(employeeId.Value, command.Comment);
    }
}
