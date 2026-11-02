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
        if (entryId.IsFailure)
            return Result.Failure(entryId.Errors.ToArray());
        if (employeeId.IsFailure)
            return Result.Failure(employeeId.Errors.ToArray());

        var entry = await repository.GetAsync(entryId.Value);
        if (entry is null)
            return Result.Failure(new Error("TimeEntryNotFound", "The time entry was not found."));

        return entry.Update(employeeId.Value, command.Date, command.Hours, command.Comment);
    }
}
