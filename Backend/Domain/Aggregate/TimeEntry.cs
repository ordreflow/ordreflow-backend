using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.ValueObjects;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Aggregate;

public sealed class TimeEntry
{
    private readonly List<TimeEntryAudit> _audits = new();
    private readonly List<TimeEntryReview> _reviews = new();
    public TimeEntryId Id { get; private set; } = null!;
    public long Revision { get; private set; }
    
    // Who belongs the timeentry to
    public EmployeeId EmployeeId { get; private set; } = null!;
    
    // Same list as first, just only in readmode
    public IReadOnlyCollection<TimeEntryAudit> Audits => _audits.AsReadOnly();
    public IReadOnlyCollection<TimeEntryReview> Reviews => _reviews.AsReadOnly();

    [NotMapped]
    public TimeEntryAudit? CurrentAudit => GetCurrentAudit();

    [NotMapped]
    public TimeEntryStatus? CurrentStatus => CurrentAudit?.Status;

    [NotMapped]
    public TaskId? CurrentTaskId => CurrentAudit?.TaskId;

    [NotMapped]
    public WorkDate? CurrentWorkDate => CurrentAudit?.WorkDate;

    [NotMapped]
    public WorkDuration? CurrentDuration => CurrentAudit?.Duration;

    public static Result<TimeEntry> Create(EmployeeId employeeId, TimeEntryAudit initialAudit)
    {
        if (employeeId is null)
            return Result<TimeEntry>.Failure(new Error("EmployeeRequired", "An employee is required."));
        if (initialAudit is null)
            return Result<TimeEntry>.Failure(new Error("AuditRequired", "An initial audit is required."));

        if (initialAudit.TimeEntryId is null)
            return Result<TimeEntry>.Failure(new Error("TimeEntryIdRequired", "The audit must identify its time entry."));

        var entry = new TimeEntry
        {
            Id = initialAudit.TimeEntryId,
            EmployeeId = employeeId
        };
        var added = entry.AddAudit(initialAudit);
        return added.IsFailure
            ? Result<TimeEntry>.Failure(added.Errors.ToArray())
            : Result<TimeEntry>.Success(entry);
    }

    public static Result<TimeEntry> Record(
        EmployeeId employeeId,
        TaskId taskId,
        WorkDate workDate,
        WorkDuration duration,
        WorkDescription description,
        WorkDatePeriod permittedPeriod,
        DateTime recordedAtUtc)
    {
        if (employeeId is null)
            return Result<TimeEntry>.Failure(new Error("EmployeeRequired", "An employee is required."));
        if (taskId is null)
            return Result<TimeEntry>.Failure(new Error("TaskRequired", "A task is required."));
        if (workDate is null || duration is null || description is null)
            return Result<TimeEntry>.Failure(new Error("TimeEntryDetailsRequired", "Date, duration and description are required."));
        if (permittedPeriod is null || !permittedPeriod.Contains(workDate))
            return Result<TimeEntry>.Failure(new Error("WorkDateNotPermitted", "The work date is outside the permitted registration period."));

        var entryId = TimeEntryId.New();
        var audit = TimeEntryAudit.Create(
            entryId,
            employeeId,
            taskId,
            workDate,
            duration,
            description,
            TimeEntryStatus.Draft,
            recordedAtUtc);
        return audit.IsFailure
            ? Result<TimeEntry>.Failure(audit.Errors.ToArray())
            : Create(employeeId, audit.Value);
    }

    public Result Submit(EmployeeId employeeId, DateTime submittedAtUtc)
    {
        var owner = EnsureOwner(employeeId);
        if (owner.IsFailure)
            return owner;

        var current = GetCurrentAudit();
        if (current?.Status != TimeEntryStatus.Draft)
            return Result.Failure(new Error("TimeEntryNotSubmittable", "Only a draft time entry can be submitted."));

        return AppendVersion(current, employeeId, TimeEntryStatus.ReadyForApproval, submittedAtUtc);
    }

    public Result Change(
        EmployeeId employeeId,
        WorkDate workDate,
        WorkDuration duration,
        WorkDescription description,
        WorkDatePeriod permittedPeriod,
        DateTime changedAtUtc)
    {
        var owner = EnsureOwner(employeeId);
        if (owner.IsFailure)
            return owner;

        var current = GetCurrentAudit();
        if (current is null || current.Status is not (TimeEntryStatus.Draft or TimeEntryStatus.Rejected))
            return Result.Failure(new Error("TimeEntryNotEditable", "The time entry is not in an editable state."));
        if (workDate is null || duration is null || description is null)
            return Result.Failure(new Error("TimeEntryDetailsRequired", "Date, duration and description are required."));
        if (permittedPeriod is null ||
            !permittedPeriod.Contains(current.WorkDate) ||
            !permittedPeriod.Contains(workDate))
            return Result.Failure(new Error("WorkDateNotPermitted", "The current and new work dates must be inside the permitted period."));

        var audit = TimeEntryAudit.Create(
            Id, employeeId, current.TaskId, workDate, duration, description, current.Status, changedAtUtc);
        return audit.IsFailure
            ? Result.Failure(audit.Errors.ToArray())
            : AddAudit(audit.Value);
    }

    public Result Delete(EmployeeId employeeId, WorkDatePeriod permittedPeriod, DateTime deletedAtUtc)
    {
        var owner = EnsureOwner(employeeId);
        if (owner.IsFailure)
            return owner;

        var current = GetCurrentAudit();
        if (current is null || current.Status is not (TimeEntryStatus.Draft or TimeEntryStatus.Rejected))
            return Result.Failure(new Error("TimeEntryNotEditable", "The time entry is not in a deletable state."));
        if (permittedPeriod is null || !permittedPeriod.Contains(current.WorkDate))
            return Result.Failure(new Error("WorkDateNotPermitted", "The work date is outside the permitted registration period."));

        var audit = TimeEntryAudit.Create(
            Id, employeeId, current.TaskId, current.WorkDate, current.Duration, current.Description,
            TimeEntryStatus.Deleted, deletedAtUtc);
        return audit.IsFailure
            ? Result.Failure(audit.Errors.ToArray())
            : AddAudit(audit.Value);
    }

    public Result Resubmit(
        EmployeeId employeeId,
        WorkDatePeriod permittedPeriod,
        DateTime submittedAtUtc)
    {
        var owner = EnsureOwner(employeeId);
        if (owner.IsFailure)
            return owner;

        var current = GetCurrentAudit();
        if (current?.Status != TimeEntryStatus.Rejected)
            return Result.Failure(new Error("TimeEntryNotRejected", "Only a rejected time entry can be resubmitted."));
        if (permittedPeriod is null || !permittedPeriod.Contains(current.WorkDate))
            return Result.Failure(new Error("WorkDateNotPermitted", "The work date is outside the permitted registration period."));

        return AppendVersion(current, employeeId, TimeEntryStatus.ReadyForApproval, submittedAtUtc);
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

    public Result Approve(EmployeeId reviewerId, DateTime reviewedAtUtc, bool allowSelfReview) =>
        Decide(reviewerId, ReviewStatus.Approved, null, reviewedAtUtc, allowSelfReview);

    public Result Reject(EmployeeId reviewerId, string? reason, DateTime reviewedAtUtc, bool allowSelfReview) =>
        Decide(reviewerId, ReviewStatus.Rejected, reason, reviewedAtUtc, allowSelfReview);

    private Result Decide(
        EmployeeId reviewerId, ReviewStatus decision, string? reason, DateTime reviewedAtUtc, bool allowSelfReview)
    {
        if (reviewerId is null)
            return Result.Failure(new Error("ReviewerRequired", "A reviewer is required."));
        if (policy is null)
            return Result.Failure(new Error("ReviewPolicyRequired", "A review policy is required."));

        var current = GetCurrentAudit();
        if (current?.Status != TimeEntryStatus.ReadyForApproval)
            return Result.Failure(new Error("TimeEntryNotReady", "Only a ready time entry can be reviewed."));
        if (!policy.AllowSelfReview && reviewerId == EmployeeId)
            return Result.Failure(new Error("SelfReviewForbidden", "Reviewing your own entry is not permitted."));

        var review = TimeEntryReview.Create(Id, reviewerId, decision, reason, reviewedAtUtc);
        if (review.IsFailure)
            return Result.Failure(review.Errors.ToArray());

        var status = decision == ReviewStatus.Approved
            ? TimeEntryStatus.Approved
            : TimeEntryStatus.Rejected;
        var audit = TimeEntryAudit.Create(
            Id,
            reviewerId,
            current.TaskId,
            current.WorkDate,
            current.Duration,
            current.Description,
            status,
            reviewedAtUtc);
        if (audit.IsFailure)
            return Result.Failure(audit.Errors.ToArray());

        var appended = AddAudit(audit.Value);
        if (appended.IsFailure)
            return appended;

        _reviews.Add(review.Value);
        return Result.Success();
    }

    private Result EnsureOwner(EmployeeId employeeId)
    {
        if (employeeId is null)
            return Result.Failure(new Error("EmployeeRequired", "An employee is required."));
        return employeeId == EmployeeId
            ? Result.Success()
            : Result.Failure(new Error("EntryOwnerMismatch", "Only the owning employee can change this time entry."));
    }

    private Result AppendVersion(
        TimeEntryAudit current,
        EmployeeId actorId,
        TimeEntryStatus status,
        DateTime auditTimeUtc)
    {
        var audit = TimeEntryAudit.Create(
            Id,
            actorId,
            current.TaskId,
            current.WorkDate,
            current.Duration,
            current.Description,
            status,
            auditTimeUtc);
        return audit.IsFailure
            ? Result.Failure(audit.Errors.ToArray())
            : AddAudit(audit.Value);
    }
}
