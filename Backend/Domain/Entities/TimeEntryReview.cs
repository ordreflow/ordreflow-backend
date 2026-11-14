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
    public string? Description { get; private set; }

    [NotMapped]
    public RejectionReason? RejectionReason =>
        Status == ReviewStatus.Rejected && !string.IsNullOrWhiteSpace(Description)
            ? Domain.ValueObjects.RejectionReason.Create(Description).Value
            : null;
    public ReviewStatus Status { get; private set; }
    public DateTime ReviewedAtUtc { get; private set; }

    private TimeEntryReview() { }

    public static Result<TimeEntryReview> Create(
        TimeEntryId timeEntryId,
        EmployeeId reviewerId,
        ReviewStatus status,
        RejectionReason? reason,
        DateTime reviewedAtUtc)
    {
        if (timeEntryId is null || reviewerId is null)
            return Result<TimeEntryReview>.Failure(new Error("ReviewIdentityRequired", "The entry and reviewer are required."));
        if (!Enum.IsDefined(status))
            return Result<TimeEntryReview>.Failure(new Error("InvalidReviewStatus", "The review status is invalid."));
        if (reviewedAtUtc.Kind != DateTimeKind.Utc)
            return Result<TimeEntryReview>.Failure(new Error("ReviewTimeUtcRequired", "The review time must be UTC."));
        if (status == ReviewStatus.Rejected && reason is null)
            return Result<TimeEntryReview>.Failure(new Error("RejectReasonRequired", "A rejection reason is required."));

        return Result<TimeEntryReview>.Success(new TimeEntryReview
        {
            Id = TimeEntryReviewId.New(),
            TimeEntryId = timeEntryId,
            ReviewerId = reviewerId,
            Status = status,
            Description = status == ReviewStatus.Rejected ? reason!.Value : null,
            ReviewedAtUtc = reviewedAtUtc
        });
    }
}
