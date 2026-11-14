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

        var reviewer = await employees.GetWithAccessesAsync(typedReviewerId.Value);
        if (reviewer is null || !reviewer.CanReviewTimeEntries())
            return Result.Failure(new Error("ReviewForbidden", "An active category reviewer permission is required."));

        var typedEntryId = TimeEntryId.Create(entryId);
        if (typedEntryId.IsFailure)
            return Result.Failure(typedEntryId.Errors.ToArray());

        var entry = await entries.GetForDecisionAsync(typedEntryId.Value);
        if (entry is null)
            return Result.Failure(new Error("TimeEntryNotFound", "The time entry was not found."));

        var current = entry.GetCurrentAudit();
        if (current is null ||
            !await entries.HasReviewerTaskAccessAsync(typedReviewerId.Value, current.TaskId))
            return Result.Failure(new Error("ReviewScopeForbidden", "The reviewer is not assigned to this category."));

        var reviewedAtUtc = clock.GetUtcNow().UtcDateTime;
        return approve
            ? entry.Approve(typedReviewerId.Value, reviewedAtUtc, policy)
            : RejectEntry(entry, typedReviewerId.Value, reason, reviewedAtUtc);
    }

    private Result RejectEntry(
        Domain.Aggregate.TimeEntry entry,
        EmployeeId reviewerId,
        string? reason,
        DateTime reviewedAtUtc)
    {
        var rejectionReason = RejectionReason.Create(reason);
        return rejectionReason.IsFailure
            ? Result.Failure(rejectionReason.Errors.ToArray())
            : entry.Reject(reviewerId, rejectionReason.Value, reviewedAtUtc, policy);
    }
}
