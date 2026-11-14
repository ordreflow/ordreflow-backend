using Application.Commands;
using Core.Tools.OperationResult;

namespace Application.Handlers;

public sealed class RejectTimeEntryHandler(TimeEntryReviewCoordinator reviews) : ICommandHandler<RejectTimeEntryCommand>
{
    public Task<Result> HandleAsync(RejectTimeEntryCommand command) =>
        reviews.RejectAsync(command.TimeEntryId, command.ReviewerId, command.Reason);
}
