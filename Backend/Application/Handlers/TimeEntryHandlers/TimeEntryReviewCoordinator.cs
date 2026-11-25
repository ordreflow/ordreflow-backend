using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;
using Domain.Services;

namespace Application.Handlers;

public sealed class TimeEntryReviewCoordinator(
    IEmployeeAccessRepository employees,
    IReviewDecisionRepository entries,
    TimeEntryReviewPolicy policy,
    TimeProvider clock,
    TimeEntryReviewDomainService reviewService)
{
    public Task<Result> ApproveAsync(Guid entryId, Guid reviewerId) =>
        DecideAsync(entryId, reviewerId, null, approve: true);

    public Task<Result> RejectAsync(Guid entryId, Guid reviewerId, string? reason) =>
        DecideAsync(entryId, reviewerId, reason, approve: false);

    private async Task<Result> DecideAsync(Guid entryId, Guid reviewerId, string? reason, bool approve)
    {
        var typedReviewerId = EmployeeId.Create(reviewerId);
        if (typedReviewerId.IsFailure)
            return Result.Failure(typedReviewerId.Errors.ToArray());

        var reviewerKey = typedReviewerId.Value;
        var reviewer = await employees.GetWithAccessesAsync(reviewerKey);
        if (reviewer is null || !reviewer.CanReviewTimeEntries())
            return Result.Failure(new Error("ReviewForbidden", "An active category reviewer permission is required."));

        var typedEntryId = TimeEntryId.Create(entryId);
        if (typedEntryId.IsFailure)
            return Result.Failure(typedEntryId.Errors.ToArray());

        var entry = await entries.GetForDecisionAsync(typedEntryId.Value);
        if (entry is null)
            return Result.Failure(new Error("TimeEntryNotFound", "The time entry was not found."));

        var current = entry.GetCurrentAudit();
        var assigned = current is not null && await entries.HasReviewerTaskAccessAsync(reviewerKey, current.TaskId);

        var reviewedAtUtc = clock.GetUtcNow().UtcDateTime;
        return approve
            ? reviewService.Approve(entry, reviewer, assigned, policy, reviewedAtUtc)
            : reviewService.Reject(entry, reviewer, assigned, policy, reviewedAtUtc, reason);
    }
}
