using Application.Commands;
using Core.Tools.OperationResult;

namespace Application.Handlers;

public sealed class ApproveTimeEntryHandler(TimeEntryReviewCoordinator reviews) : ICommandHandler<ApproveTimeEntryCommand>
{
    public Task<Result> HandleAsync(ApproveTimeEntryCommand command) =>
        reviews.ApproveAsync(command.TimeEntryId, command.ReviewerId);
}
