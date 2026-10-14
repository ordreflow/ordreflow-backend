using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class DeleteTimeEntryHandler(
    ITimeEntryRepository timeEntryRepository) : ICommandHandler<DeleteTimeEntryCommand>
{
    public async Task<Result> HandleAsync(DeleteTimeEntryCommand command)
    {
        var entryIdResult = TimeEntryId.Create(command.TimeEntryId);

        if (entryIdResult.IsFailure)
            return Result.Failure(entryIdResult.Errors.ToArray());

        var employeeIdResult = UserId.Create(command.EmployeeId);

        if (employeeIdResult.IsFailure)
            return Result.Failure(employeeIdResult.Errors.ToArray());

        var entry = await timeEntryRepository.GetAsync(entryIdResult.Value);

        if (entry is null)
            return Result.Failure(new Error("TimeEntryNotFound", "The time entry was not found."));

        var editCheck = entry.CanEdit(employeeIdResult.Value);

        if (editCheck.IsFailure)
            return editCheck;

        await timeEntryRepository.RemoveAsync(entryIdResult.Value);

        return Result.Success();
    }
}
