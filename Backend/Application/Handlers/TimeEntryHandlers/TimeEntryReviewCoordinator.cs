using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class TimeEntryReviewCoordinator(
    IEmployeeAccessRepository employees,
    IReviewDecisionRepository entries,
    TimeEntryReviewPolicy policy,
    TimeProvider clock)
{
    public Task<Result> ApproveAsync(Guid entryId, Guid reviewerId) =>
        DecideAsync(entryId, reviewerId, null, approve: true);

    public Task<Result> RejectAsync(Guid entryId, Guid reviewerId, string? reason) =>
        DecideAsync(entryId, reviewerId, reason, approve: false);

    private async Task<Result> DecideAsync(Guid entryId, Guid reviewerId, string? reason, bool approve)
    {
        if (reviewerId == Guid.Empty)
            return Result.Failure(new Error("ReviewerRequired", "A reviewer is required."));

        var reviewer = await employees.GetWithAccessesAsync(reviewerId);
        if (reviewer is null || !reviewer.CanReviewTimeEntries())
            return Result.Failure(new Error("ReviewForbidden", "An active category reviewer permission is required."));

        if (entryId == Guid.Empty)
            return Result.Failure(new Error("TimeEntryIdRequired", "A time entry is required."));

        var entry = await entries.GetForDecisionAsync(entryId);
        if (entry is null)
            return Result.Failure(new Error("TimeEntryNotFound", "The time entry was not found."));

        var current = entry.GetCurrentAudit();
        if (current is null || !await entries.HasReviewerTaskAccessAsync(reviewerId, current.TaskId))
            return Result.Failure(new Error("ReviewScopeForbidden", "The reviewer is not assigned to this category."));

        var reviewedAtUtc = clock.GetUtcNow().UtcDateTime;
        return approve
            ? entry.Approve(reviewerId, reviewedAtUtc, policy.AllowSelfReview)
            : entry.Reject(reviewerId, reason, reviewedAtUtc, policy.AllowSelfReview);
    }
}
