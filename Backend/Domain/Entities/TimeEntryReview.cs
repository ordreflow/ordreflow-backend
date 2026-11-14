using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Entities;

/*  Summary
 *  when a Time entry is reviewed
 */
public sealed class TimeEntryReview
{
    public Guid Id { get; private set; }
    public Guid TimeEntryId { get; private set; }
    public Guid ReviewerId { get; private set; }
    public string? Description { get; private set; }
    public ReviewStatus Status { get; private set; }
    public DateTime ReviewedAtUtc { get; private set; }

    private TimeEntryReview() { }

    public static Result<TimeEntryReview> Create(
        Guid timeEntryId, Guid reviewerId, ReviewStatus status, string? reason, DateTime reviewedAtUtc)
    {
        if (timeEntryId == Guid.Empty || reviewerId == Guid.Empty)
            return Result<TimeEntryReview>.Failure(new Error("ReviewIdentityRequired", "The entry and reviewer are required."));
        if (!Enum.IsDefined(status))
            return Result<TimeEntryReview>.Failure(new Error("InvalidReviewStatus", "The review status is invalid."));
        if (reviewedAtUtc.Kind != DateTimeKind.Utc)
            return Result<TimeEntryReview>.Failure(new Error("ReviewTimeUtcRequired", "The review time must be UTC."));
        if (status == ReviewStatus.Rejected && string.IsNullOrWhiteSpace(reason))
            return Result<TimeEntryReview>.Failure(new Error("RejectReasonRequired", "A rejection reason is required."));

        return Result<TimeEntryReview>.Success(new TimeEntryReview
        {
            Id = Guid.NewGuid(),
            TimeEntryId = timeEntryId,
            ReviewerId = reviewerId,
            Status = status,
            Description = status == ReviewStatus.Rejected ? reason!.Trim() : null,
            ReviewedAtUtc = reviewedAtUtc
        });
    }
}
