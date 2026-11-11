using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Aggregate;

public sealed class TimeEntry
{
    private readonly List<TimeEntryReview> _reviews = new();
    private readonly List<TimeEntryAuditEvent> _auditEvents = new();

    public TimeEntryId Id { get; private set; } = null!;
    public UserId EmployeeId { get; private set; } = null!;
    public TaskId TaskId { get; private set; } = null!;
    public DateOnly WorkDate { get; private set; }
    public int DurationMinutes { get; private set; }
    public string? Comment { get; private set; }
    public TimeEntryStatus Status { get; private set; } = TimeEntryStatus.Draft;
    public IReadOnlyCollection<TimeEntryReview> Reviews => _reviews.AsReadOnly();
    public IReadOnlyCollection<TimeEntryAuditEvent> AuditEvents => _auditEvents.AsReadOnly();

    private TimeEntry()
    {
    }

    private TimeEntry(
        UserId employeeId,
        TaskId taskId,
        DateOnly workDate,
        int durationMinutes,
        string? comment)
    {
        Id = TimeEntryId.Create(Guid.NewGuid()).Value;
        EmployeeId = employeeId;
        TaskId = taskId;
        WorkDate = workDate;
        DurationMinutes = durationMinutes;
        Comment = comment;
    }

    public static Result<TimeEntry> Create(
        UserId employeeId,
        TaskId taskId,
        DateOnly workDate,
        int durationMinutes,
        string? comment)
    {
        if (employeeId is null)
            return Result<TimeEntry>.Failure(new Error("EmployeeRequired", "An employee is required."));

        if (taskId is null)
            return Result<TimeEntry>.Failure(new Error("TaskRequired", "A task is required."));

        var validation = ValidateValues(workDate, durationMinutes, comment);
        if (validation.IsFailure)
            return Result<TimeEntry>.Failure(validation.Errors.ToArray());

        var entry = new TimeEntry(
            employeeId,
            taskId,
            workDate,
            durationMinutes,
            comment?.Trim());

        var auditResult = entry.AppendAuditEvent(
            employeeId,
            TimeEntryAuditEventType.Created,
            "Time entry created.");

        return auditResult.IsFailure
            ? Result<TimeEntry>.Failure(auditResult.Errors.ToArray())
            : Result<TimeEntry>.Success(entry);
    }

    public Result Submit(UserId actorId)
    {
        if (actorId is null || actorId != EmployeeId)
            return Result.Failure(new Error("EntryOwnerMismatch", "Only the employee can submit this time entry."));

        if (Status != TimeEntryStatus.Draft)
            return Result.Failure(new Error("TimeEntryNotDraft", "Only a draft time entry can be submitted."));

        Status = TimeEntryStatus.Submitted;
        return AppendAuditEvent(actorId, TimeEntryAuditEventType.Submitted, "Time entry submitted.");
    }

    public Result Correct(
        UserId actorId,
        DateOnly workDate,
        int durationMinutes,
        string? comment)
    {
        if (actorId is null || actorId != EmployeeId)
            return Result.Failure(new Error("EntryOwnerMismatch", "Only the employee can correct this time entry."));

        if (Status != TimeEntryStatus.Returned)
            return Result.Failure(new Error("TimeEntryNotReturned", "Only a returned time entry can be corrected."));

        var validation = ValidateValues(workDate, durationMinutes, comment);
        if (validation.IsFailure)
            return validation;

        WorkDate = workDate;
        DurationMinutes = durationMinutes;
        Comment = comment?.Trim();
        Status = TimeEntryStatus.Draft;
        return AppendAuditEvent(actorId, TimeEntryAuditEventType.Corrected, "Returned time entry corrected.");
    }

    public Result Accept(
        UserId reviewerId,
        UserRole reviewerRole,
        UserStatus reviewerStatus,
        DepartmentId? reviewerDepartmentId,
        UserId employeeId,
        DepartmentId? employeeDepartmentId)
    {
        var reviewResult = CanReview(
            reviewerId,
            reviewerRole,
            reviewerStatus,
            reviewerDepartmentId,
            employeeId,
            employeeDepartmentId);
        if (reviewResult.IsFailure)
            return reviewResult;

        if (Status != TimeEntryStatus.Submitted)
            return Result.Failure(new Error("TimeEntryNotSubmitted", "Only a submitted time entry can be accepted."));

        var review = TimeEntryReview.Create(Id, ReviewDecision.Accepted, null);
        if (review.IsFailure)
            return Result.Failure(review.Errors.ToArray());

        Status = TimeEntryStatus.Accepted;
        _reviews.Add(review.Value);
        return AppendAuditEvent(reviewerId, TimeEntryAuditEventType.Reviewed, "Time entry accepted.");
    }

    public Result Return(
        UserId reviewerId,
        UserRole reviewerRole,
        UserStatus reviewerStatus,
        DepartmentId? reviewerDepartmentId,
        UserId employeeId,
        DepartmentId? employeeDepartmentId,
        string reason)
    {
        var reviewResult = CanReview(
            reviewerId,
            reviewerRole,
            reviewerStatus,
            reviewerDepartmentId,
            employeeId,
            employeeDepartmentId);
        if (reviewResult.IsFailure)
            return reviewResult;

        if (Status != TimeEntryStatus.Submitted)
            return Result.Failure(new Error("TimeEntryNotSubmitted", "Only a submitted time entry can be returned."));

        var review = TimeEntryReview.Create(Id, ReviewDecision.Returned, reason);
        if (review.IsFailure)
            return Result.Failure(review.Errors.ToArray());

        Status = TimeEntryStatus.Returned;
        _reviews.Add(review.Value);
        return AppendAuditEvent(reviewerId, TimeEntryAuditEventType.Reviewed, "Time entry returned.");
    }

    public Result Finalize(
        UserId reviewerId,
        UserRole reviewerRole,
        UserStatus reviewerStatus,
        DepartmentId? reviewerDepartmentId,
        UserId employeeId,
        DepartmentId? employeeDepartmentId)
    {
        var reviewResult = CanReview(
            reviewerId,
            reviewerRole,
            reviewerStatus,
            reviewerDepartmentId,
            employeeId,
            employeeDepartmentId);
        if (reviewResult.IsFailure)
            return reviewResult;

        if (Status != TimeEntryStatus.Accepted)
            return Result.Failure(new Error("TimeEntryNotAccepted", "Only an accepted time entry can be finalized."));

        var review = TimeEntryReview.Create(Id, ReviewDecision.Finalized, null);
        if (review.IsFailure)
            return Result.Failure(review.Errors.ToArray());

        Status = TimeEntryStatus.Finalized;
        _reviews.Add(review.Value);
        return AppendAuditEvent(reviewerId, TimeEntryAuditEventType.Finalized, "Time entry finalized.");
    }

    public bool CanEdit(UserId actorId) =>
        actorId is not null &&
        actorId == EmployeeId &&
        Status is TimeEntryStatus.Draft or TimeEntryStatus.Returned;

    public bool CanView(
        UserId actorId,
        UserRole actorRole,
        UserStatus actorStatus,
        DepartmentId? actorDepartmentId,
        DepartmentId? employeeDepartmentId)
    {
        if (actorId is null || actorStatus != UserStatus.Active)
            return false;

        if (actorId == EmployeeId)
            return true;

        if (actorRole == UserRole.Admin)
            return true;

        return actorRole == UserRole.Manager &&
            actorDepartmentId is not null &&
            actorDepartmentId == employeeDepartmentId;
    }

    private Result CanReview(
        UserId reviewerId,
        UserRole reviewerRole,
        UserStatus reviewerStatus,
        DepartmentId? reviewerDepartmentId,
        UserId employeeId,
        DepartmentId? employeeDepartmentId)
    {
        if (employeeId is null || employeeId != EmployeeId)
            return Result.Failure(new Error("EmployeeMismatch", "The employee does not own this time entry."));

        if (reviewerId is null ||
            reviewerStatus != UserStatus.Active ||
            reviewerRole is not (UserRole.Manager or UserRole.Admin))
            return Result.Failure(new Error("ReviewForbidden", "Only an active manager or admin can review time entries."));

        if (reviewerRole == UserRole.Manager &&
            (reviewerDepartmentId is null ||
             reviewerDepartmentId != employeeDepartmentId))
            return Result.Failure(new Error("ReviewScopeForbidden", "The manager cannot review this employee's time entry."));

        return Result.Success();
    }

    private Result AppendAuditEvent(
        UserId actorId,
        TimeEntryAuditEventType eventType,
        string relevantChanges)
    {
        var auditEvent = TimeEntryAuditEvent.Create(Id, actorId, eventType, relevantChanges);
        if (auditEvent.IsFailure)
            return Result.Failure(auditEvent.Errors.ToArray());

        _auditEvents.Add(auditEvent.Value);
        return Result.Success();
    }

    private static Result ValidateValues(
        DateOnly workDate,
        int durationMinutes,
        string? comment)
    {
        if (workDate == default)
            return Result.Failure(new Error("InvalidDate", "Work date is required."));

        if (durationMinutes is <= 0 or > 1440)
            return Result.Failure(new Error("InvalidDuration", "Duration must be greater than 0 and cannot exceed 1440 minutes."));

        if (comment?.Trim().Length > 1000)
            return Result.Failure(new Error("CommentTooLong", "Comment cannot exceed 1000 characters."));

        return Result.Success();
    }
}
