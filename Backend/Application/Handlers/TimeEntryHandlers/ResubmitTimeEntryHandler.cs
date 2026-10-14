using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class ResubmitTimeEntryHandler(
    ITimeEntryRepository timeEntryRepository) : ICommandHandler<ResubmitTimeEntryCommand>
{
    public async Task<Result> HandleAsync(ResubmitTimeEntryCommand command)
    {
        var entryId = TimeEntryId.Create(command.TimeEntryId);
        var employeeId = UserId.Create(command.EmployeeId);
        if (entryId.IsFailure || employeeId.IsFailure)
            return Result.Failure((entryId.IsFailure ? entryId.Errors : employeeId.Errors).ToArray());

        var entry = await timeEntryRepository.GetAsync(entryId.Value);
        if (entry is null)
            return Result.Failure(new Error("TimeEntryNotFound", "The time entry was not found."));

        return entry.Resubmit(employeeId.Value);
    }
}
