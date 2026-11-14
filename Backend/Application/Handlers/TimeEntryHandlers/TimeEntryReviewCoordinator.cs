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
        var typedReviewerId = EmployeeId.Create(reviewerId);
        if (typedReviewerId.IsFailure)
            return Result.Failure(typedReviewerId.Errors.ToArray());

        var reviewerKey = EmployeeId.Create(reviewerId).Value;
        var reviewer = await employees.GetWithAccessesAsync(reviewerKey);
        if (reviewer is null || !reviewer.CanReviewTimeEntries())
            return Result.Failure(new Error("ReviewForbidden", "An active category reviewer permission is required."));

        var typedEntryId = TimeEntryId.Create(entryId);
        if (typedEntryId.IsFailure)
            return Result.Failure(typedEntryId.Errors.ToArray());

        var entry = await entries.GetForDecisionAsync(TimeEntryId.Create(entryId).Value);
        if (entry is null)
            return Result.Failure(new Error("TimeEntryNotFound", "The time entry was not found."));

        var current = entry.GetCurrentAudit();
        if (current is null || !await entries.HasReviewerTaskAccessAsync(reviewerKey, current.TaskId))
            return Result.Failure(new Error("ReviewScopeForbidden", "The reviewer is not assigned to this category."));

        var reviewedAtUtc = clock.GetUtcNow().UtcDateTime;
        return approve
            ? entry.Approve(reviewerKey, reviewedAtUtc, policy.AllowSelfReview)
            : entry.Reject(reviewerKey, reason, reviewedAtUtc, policy.AllowSelfReview);
    }
}
