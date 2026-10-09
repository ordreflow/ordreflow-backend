using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Aggregate;

public sealed class TimeEntry
{
    private readonly List<TimeEntryReview> _reviews = new();

    public  TimeEntryId Id { get; private set; }

    public UserId EmployeeId { get; private set; } = null!;

    public TaskId TaskId { get; private set; } = null!;

    public DateTime Date { get; private set; }

    public decimal Hours { get; private set; }

    public string? Comment { get; private set; }


    public TimeEntryStatus Status { get; private set; } = TimeEntryStatus.Draft;

    public IReadOnlyCollection<TimeEntryReview> Reviews =>
        _reviews.AsReadOnly();

    private TimeEntry()
    {
    }

    private TimeEntry(
        UserId employeeId,
        TaskId taskId,
        DateTime date,
        decimal hours,
        string? comment)
    {
        Id = TimeEntryId.Create(Guid.NewGuid()).Value;
        EmployeeId = employeeId;
        TaskId = taskId;
        Date = date;
        Hours = hours;
        Comment = comment;
    }

    public static Result<TimeEntry> Create(
        UserId employeeId,
        TaskId taskId,
        DateTime date,
        decimal hours,
        string? comment)
    {
        if (employeeId is null)
            return Result<TimeEntry>.Failure(
                new Error("EmployeeRequired", "An employee is required."));

        if (taskId is null)
            return Result<TimeEntry>.Failure(
                new Error("WorkItemRequired", "A work item is required."));

        if (date == default)
            return Result<TimeEntry>.Failure(
                new Error("InvalidDate", "Date is required."));

        if (hours <= 0 || hours > 24)
            return Result<TimeEntry>.Failure(
                new Error(
                    "InvalidHours",
                    "Hours must be greater than 0 and cannot exceed 24."));

        comment = comment?.Trim();

        if (comment?.Length > 1000)
            return Result<TimeEntry>.Failure(
                new Error(
                    "CommentTooLong",
                    "Comment cannot exceed 1000 characters."));

        return Result<TimeEntry>.Success(
            new TimeEntry(
                employeeId,
                taskId,
                date,
                hours,
                comment));
    }

    public Result ChangeHours(UserId actorId, decimal hours)
    {
        var editResult = CanEdit(actorId);

        if (editResult.IsFailure)
            return editResult;

        if (hours <= 0 || hours > 24)
            return Result.Failure(
                new Error(
                    "InvalidHours",
                    "Hours must be greater than 0 and cannot exceed 24."));

        Hours = hours;

        return Result.Success();
    }

    public Result ChangeDate(UserId actorId, DateTime date)
    {
        var editResult = CanEdit(actorId);

        if (editResult.IsFailure)
            return editResult;

        if (date == default)
            return Result.Failure(
                new Error("InvalidDate", "Date is required."));

        Date = date;

        return Result.Success();
    }

    public Result ChangeComment(UserId actorId, string? comment)
    {
        var editResult = CanEdit(actorId);

        if (editResult.IsFailure)
            return editResult;

        comment = comment?.Trim();

        if (comment?.Length > 1000)
            return Result.Failure(
                new Error(
                    "CommentTooLong",
                    "Comment cannot exceed 1000 characters."));

        Comment = comment;

        return Result.Success();
    }

    public Result Accept(
        UserId reviewerId,
        UserId employeeId,
        UserId? employeeManagerId,
        UserRole reviewerRole,
        UserStatus reviewerStatus)
    {
        var reviewResult = CanReview(
            reviewerId,
            employeeId,
            employeeManagerId,
            reviewerRole,
            reviewerStatus);

        if (reviewResult.IsFailure)
            return reviewResult;

        if (Status != TimeEntryStatus.Draft)
            return Result.Failure(
                new Error(
                    "TimeEntryNotOpen",
                    "Only an open time entry can be accepted."));

        var review = TimeEntryReview.Create(
            Id,
            ReviewDecision.Accepted,
            null);

        if (review.IsFailure)
            return Result.Failure(review.Errors.ToArray());

        Status = TimeEntryStatus.Accepted;

        _reviews.Add(review.Value);

        return Result.Success();
    }

    public Result Return(
        UserId reviewerId,
        UserId employeeId,
        UserId? employeeManagerId,
        UserRole reviewerRole,
        UserStatus reviewerStatus,
        string reason)
    {
        var reviewResult = CanReview(
            reviewerId,
            employeeId,
            employeeManagerId,
            reviewerRole,
            reviewerStatus);

        if (reviewResult.IsFailure)
            return reviewResult;

        if (Status != TimeEntryStatus.Draft)
            return Result.Failure(
                new Error(
                    "TimeEntryNotOpen",
                    "Only an open time entry can be returned."));

        var review = TimeEntryReview.Create(
            Id,
            ReviewDecision.Returned,
            reason);

        if (review.IsFailure)
            return Result.Failure(review.Errors.ToArray());

        Status = TimeEntryStatus.Returned;

        _reviews.Add(review.Value);

        return Result.Success();
    }

    public Result Resubmit(UserId actorId)
    {
        var editResult = CanEdit(actorId);

        if (editResult.IsFailure)
            return editResult;

        if (Status != TimeEntryStatus.Returned)
            return Result.Failure(
                new Error(
                    "TimeEntryNotReturned",
                    "Only a returned time entry can be resubmitted."));

        Status = TimeEntryStatus.Draft;

        return Result.Success();
    }

    public Result Finalize(
        UserId reviewerId,
        UserId employeeId,
        UserId? employeeManagerId,
        UserRole reviewerRole,
        UserStatus reviewerStatus)
    {
        var reviewResult = CanReview(
            reviewerId,
            employeeId,
            employeeManagerId,
            reviewerRole,
            reviewerStatus);

        if (reviewResult.IsFailure)
            return reviewResult;

        if (Status != TimeEntryStatus.Accepted)
            return Result.Failure(
                new Error(
                    "TimeEntryNotAccepted",
                    "Only an accepted time entry can be finalized."));

        var review = TimeEntryReview.Create(
            Id,
            ReviewDecision.Finalized,
            null);

        if (review.IsFailure)
            return Result.Failure(review.Errors.ToArray());

        Status = TimeEntryStatus.Finalized;

        _reviews.Add(review.Value);

        return Result.Success();
    }

    public Result CanEdit(UserId actorId)
    {
        if (actorId is null || actorId != EmployeeId)
            return Result.Failure(
                new Error(
                    "EntryOwnerMismatch",
                    "Only the employee can edit this time entry."));

        if (Status is not (TimeEntryStatus.Draft or TimeEntryStatus.Returned))
            return Result.Failure(
                new Error(
                    "TimeEntryNotEditable",
                    "Only open or returned time entries can be edited."));

        return Result.Success();
    }

    public bool CanView(
        UserId actorId,
        UserRole actorRole,
        UserId? employeeManagerId)
    {
        if (actorId is not null && actorId == EmployeeId)
            return true;

        if (actorRole == UserRole.Admin)
            return true;

        return actorRole == UserRole.Manager && employeeManagerId == actorId;
    }

    private Result CanReview(
        UserId reviewerId,
        UserId employeeId,
        UserId? employeeManagerId,
        UserRole reviewerRole,
        UserStatus reviewerStatus)
    {
        if (employeeId is null || employeeId != EmployeeId)
            return Result.Failure(
                new Error(
                    "EmployeeMismatch",
                    "The employee does not own this time entry."));

        if (reviewerStatus != UserStatus.Active ||
            reviewerRole is not (UserRole.Manager or UserRole.Admin))
            return Result.Failure(
                new Error(
                    "ReviewForbidden",
                    "Only an active manager or admin can review time entries."));

        if (reviewerId is null)
            return Result.Failure(
                new Error(
                    "ReviewerRequired",
                    "A reviewer is required."));

        if (reviewerRole == UserRole.Manager &&
            employeeManagerId != reviewerId)
            return Result.Failure(
                new Error(
                    "ReviewScopeForbidden",
                    "The manager cannot review this employee's time entry."));

        return Result.Success();
    }
}