using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.ValueObjects;

namespace Domain.Services;

public sealed class TimeEntryReviewDomainService
{
    public Result Approve(TimeEntry entry, Employee reviewer, bool assignedToCurrentCategory, TimeEntryReviewPolicy policy, DateTime atUtc) =>
        Decide(entry, reviewer, assignedToCurrentCategory, policy, atUtc, true, null);

    public Result Reject(TimeEntry entry, Employee reviewer, bool assignedToCurrentCategory, TimeEntryReviewPolicy policy, DateTime atUtc, string? reason) =>
        Decide(entry, reviewer, assignedToCurrentCategory, policy, atUtc, false, reason);

    private static Result Decide(TimeEntry entry, Employee reviewer, bool assigned, TimeEntryReviewPolicy policy, DateTime atUtc, bool approve, string? reason)
    {
        if (reviewer is null || !reviewer.CanReviewTimeEntries())
            return Result.Failure(new Error("ReviewForbidden", "An active category reviewer permission is required."));
        if (entry is null) return Result.Failure(new Error("TimeEntryNotFound", "The time entry was not found."));
        if (policy is null) return Result.Failure(new Error("ReviewPolicyRequired", "A review policy is required."));
        if (!assigned) return Result.Failure(new Error("ReviewScopeForbidden", "The reviewer is not assigned to this category."));
        return approve ? entry.Approve(reviewer.Id, atUtc, policy.AllowSelfReview)
            : entry.Reject(reviewer.Id, reason, atUtc, policy.AllowSelfReview);
    }
}
