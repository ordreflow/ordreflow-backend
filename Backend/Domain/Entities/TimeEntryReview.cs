using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class TimeEntryReview
{
    public TimeEntryReviewID Id { get; private set; }
    public TimeEntryId TimeEntryId { get; private set; }
    public ReviewDecision Decision { get; private set; }
    public string? Reason { get; private set; }
    public DateTime ReviewedAt { get; private set; }

    private TimeEntryReview()
    {
    }

    private TimeEntryReview(
        TimeEntryId timeEntryId,
        ReviewDecision decision,
        string? reason)
    {
        Id = TimeEntryReviewID.Create(Guid.NewGuid()).Value;
        TimeEntryId = timeEntryId;
        Decision = decision;
        Reason = reason;
        ReviewedAt = DateTime.UtcNow;
    }

    public static Result<TimeEntryReview> Create(
        TimeEntryId timeEntryId,
        ReviewDecision decision,
        string? reason)
    {
       

        if (decision == ReviewDecision.Returned && string.IsNullOrWhiteSpace(reason))
            return Result<TimeEntryReview>.Failure(
                new Error("ReturnReasonRequired", "A reason is required when returning a time entry."));

        reason = reason?.Trim();

        return Result<TimeEntryReview>.Success(
            new TimeEntryReview(timeEntryId, decision, reason));
    }
}
