using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Aggregate;

/*  Summary
 *  TimeEntries is the employeees timeregistrations and only stores the relevant information which can be changed.
 *  The rest will be in TimeEntryAudit, so that changes can be auditted
 */
public sealed class TimeEntry
{
    //list of all the Audits (versions/changes) that has been made in the timeentry
    private readonly List<TimeEntryAudit> _audits = new();
    private readonly List<TimeEntryReview> _reviews = new();
    public Guid Id { get; private set; }
    public long Revision { get; private set; }
    
    // Who belongs the timeentry to
    public Guid EmployeeId { get; private set; }
    
    // Same list as first, just only in readmode
    public IReadOnlyCollection<TimeEntryAudit> Audits => _audits.AsReadOnly();
    public IReadOnlyCollection<TimeEntryReview> Reviews => _reviews.AsReadOnly();

    private TimeEntry()
    {
    }

    public static Result<TimeEntry> Create(Guid employeeId, TimeEntryAudit initialAudit)
    {
        if (employeeId == Guid.Empty)
            return Result<TimeEntry>.Failure(new Error("EmployeeRequired", "An employee is required."));

        if (initialAudit is null)
            return Result<TimeEntry>.Failure(new Error("AuditRequired", "An initial audit is required."));

        if (initialAudit.TimeEntryId == Guid.Empty)
            return Result<TimeEntry>.Failure(new Error("TimeEntryIdRequired", "The audit must identify its time entry."));

        var entry = new TimeEntry
        {
            Id = initialAudit.TimeEntryId,
            EmployeeId = employeeId
        };

        var result = entry.AddAudit(initialAudit);
        return result.IsFailure
            ? Result<TimeEntry>.Failure(result.Errors.ToArray())
            : Result<TimeEntry>.Success(entry);
    }

    public Result AddAudit(TimeEntryAudit audit)
    {
        if (audit is null)
            return Result.Failure(new Error("AuditRequired", "An audit is required."));

        if (audit.TimeEntryId != Id)
            return Result.Failure(new Error("AuditEntryMismatch", "The audit belongs to a different time entry."));

        if (audit.AuditTime == default)
            return Result.Failure(new Error("AuditTimeRequired", "The audit time is required."));

        var current = GetCurrentAudit();
        if (current is not null && audit.AuditTime <= current.AuditTime)
            return Result.Failure(new Error("AuditTimeNotIncreasing", "The audit must be later than the current version."));

        _audits.Add(audit);
        Revision++;
        return Result.Success();
    }

    public TimeEntryAudit? GetCurrentAudit() => _audits.MaxBy(audit => audit.AuditTime);

    public Result Approve(Guid reviewerId, DateTime reviewedAtUtc, bool allowSelfReview) =>
        Decide(reviewerId, ReviewStatus.Approved, null, reviewedAtUtc, allowSelfReview);

    public Result Reject(Guid reviewerId, string? reason, DateTime reviewedAtUtc, bool allowSelfReview) =>
        Decide(reviewerId, ReviewStatus.Rejected, reason, reviewedAtUtc, allowSelfReview);

    private Result Decide(
        Guid reviewerId, ReviewStatus decision, string? reason, DateTime reviewedAtUtc, bool allowSelfReview)
    {
        var current = GetCurrentAudit();
        if (current is null || current.Status != TimeEntryStatus.ReadyForApproval)
            return Result.Failure(new Error("TimeEntryNotReady", "Only a ready time entry can be reviewed."));

        if (!allowSelfReview && reviewerId == EmployeeId)
            return Result.Failure(new Error("SelfReviewForbidden", "Reviewing your own entry is not permitted."));

        var review = TimeEntryReview.Create(Id, reviewerId, decision, reason, reviewedAtUtc);
        if (review.IsFailure)
            return Result.Failure(review.Errors.ToArray());

        var audit = TimeEntryAudit.Create(
            Id, reviewerId, current.TaskId, current.WorkDate, current.Duration,
            current.Description, decision == ReviewStatus.Approved ? TimeEntryStatus.Approved : TimeEntryStatus.Rejected,
            reviewedAtUtc);
        if (audit.IsFailure)
            return Result.Failure(audit.Errors.ToArray());

        var appended = AddAudit(audit.Value);
        if (appended.IsFailure)
            return appended;

        _reviews.Add(review.Value);
        return Result.Success();
    }
}
