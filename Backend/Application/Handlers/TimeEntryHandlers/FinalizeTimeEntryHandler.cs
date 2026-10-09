using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.Services;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class FinalizeTimeEntryHandler(
    ITimeEntryRepository timeEntryRepository,
    IUserRepository userRepository,
    TimeEntryReviewDomainService reviewService) : ICommandHandler<FinalizeTimeEntryCommand>
{
    public async Task<Result> HandleAsync(FinalizeTimeEntryCommand command)
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

        return reviewService.Finalize(entry, employee, reviewer);
    }
}
