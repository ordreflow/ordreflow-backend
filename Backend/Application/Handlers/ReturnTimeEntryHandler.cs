using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class ReturnTimeEntryHandler(
    ITimeEntryRepository timeEntryRepository,
    IUserRepository userRepository) : ICommandHandler<ReturnTimeEntryCommand>
{
    public async Task<Result> HandleAsync(ReturnTimeEntryCommand command)
    {
        var entryId = TimeEntryId.Create(command.TimeEntryId);
        var reviewerId = UserId.Create(command.ReviewerId);
        if (entryId.IsFailure || reviewerId.IsFailure)
            return Result.Failure((entryId.IsFailure ? entryId.Errors : reviewerId.Errors).ToArray());

        var entry = await timeEntryRepository.GetAsync(entryId.Value);
        if (entry is null)
            return Result.Failure(new Error("TimeEntryNotFound", "The time entry was not found."));

        var employee = await userRepository.GetAsync(entry.EmployeeId);
        var reviewer = await userRepository.GetAsync(reviewerId.Value);
        if (employee is null || reviewer is null)
            return Result.Failure(new Error("UserNotFound", "The employee or reviewer was not found."));

        return entry.Return(
            reviewer.UserId,
            employee.UserId,
            employee.ManagerId,
            reviewer.Role,
            reviewer.Status,
            command.Reason);
    }
}
