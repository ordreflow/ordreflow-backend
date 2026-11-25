using Core.Tools.OperationResult;
using Domain.ValueObjects;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

/*  Summary
 *  when a Time entry is reviewed
 */
public sealed class TimeEntryReview
{
    public TimeEntryReviewId Id { get; private set; } = null!;
    public TimeEntryId TimeEntryId { get; private set; } = null!;
    public EmployeeId ReviewerId { get; private set; } = null!;
    public RejectionReason? Description { get; private set; }
    public ReviewStatus Status { get; private set; }
    public DateTime ReviewedAtUtc { get; private set; }

    private TimeEntryReview() { }

    public static Result<TimeEntryReview> Create(
        TimeEntryId timeEntryId, EmployeeId reviewerId, ReviewStatus status, string? reason, DateTime reviewedAtUtc)
    {
        if (timeEntryId is null || reviewerId is null)
            return Result<TimeEntryReview>.Failure(new Error("ReviewIdentityRequired", "The entry and reviewer are required."));
        if (!Enum.IsDefined(status))
            return Result<TimeEntryReview>.Failure(new Error("InvalidReviewStatus", "The review status is invalid."));
        if (reviewedAtUtc.Kind != DateTimeKind.Utc)
            return Result<TimeEntryReview>.Failure(new Error("ReviewTimeUtcRequired", "The review time must be UTC."));
        RejectionReason? rejectionReason = null;
        if (status == ReviewStatus.Rejected)
        {
            var parsedReason = RejectionReason.Create(reason);
            if (parsedReason.IsFailure)
                return Result<TimeEntryReview>.Failure(parsedReason.Errors.ToArray());
            rejectionReason = parsedReason.Value;
        }

        return Result<TimeEntryReview>.Success(new TimeEntryReview
        {
            Id = TimeEntryReviewId.Create(Guid.NewGuid()).Value,
            TimeEntryId = timeEntryId,
            ReviewerId = reviewerId,
            Status = status,
            Description = rejectionReason,
            ReviewedAtUtc = reviewedAtUtc
        });
    }
}
