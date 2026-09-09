using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class TimeEntry
{
    public int Id { get; private set; }

    public WorkId WorkId { get; private set; } = null!;

    public TimeSheetId? TimeSheetId { get; private set; }

    public DateTime Date { get; private set; }

    public decimal Hours { get; private set; }

    public TimeSpan? StartTime { get; private set; }

    public TimeSpan? EndTime { get; private set; }

    public string? Comment { get; private set; }

    public TimeEntryStatus Status { get; private set; } = TimeEntryStatus.Draft;

    private TimeEntry()
    {
    }

    private TimeEntry(
        WorkId workId,
        DateTime date,
        decimal hours,
        TimeSpan? startTime,
        TimeSpan? endTime,
        string? comment)
    {
        WorkId = workId;
        Date = date;
        Hours = hours;
        StartTime = startTime;
        EndTime = endTime;
        Comment = comment;
    }

    public static Result<TimeEntry> Create(
        WorkId workId,
        DateTime date,
        decimal hours,
        TimeSpan? startTime,
        TimeSpan? endTime,
        string? comment)
    {
        if (workId is null)
            return Result<TimeEntry>.Failure(new Error("WorkItemRequired", "A work item is required."));

        if (date == default)
            return Result<TimeEntry>.Failure(new Error("InvalidDate", "Date is required."));

        if (hours <= 0 || hours > 24)
            return Result<TimeEntry>.Failure(new Error("InvalidHours", "Hours must be greater than 0 and cannot exceed 24."));

        var timeRangeResult = ValidateTimeRange(startTime, endTime);
        if (timeRangeResult.IsFailure)
            return Result<TimeEntry>.Failure(timeRangeResult.Errors.ToArray());

        comment = comment?.Trim();
        if (comment?.Length > 1000)
            return Result<TimeEntry>.Failure(new Error("CommentTooLong", "Comment cannot exceed 1000 characters."));

        return Result<TimeEntry>.Success(new TimeEntry(
            workId,
            date,
            hours,
            startTime,
            endTime,
            comment));
    }

    public Result AssignTo(TimeSheetId timeSheetId)
    {
        if (timeSheetId is null)
            return Result.Failure(new Error("TimeSheetRequired", "A timesheet is required."));

        if (TimeSheetId is not null && TimeSheetId != timeSheetId)
            return Result.Failure(new Error("TimeSheetAlreadyAssigned", "Time entry already belongs to another timesheet."));

        TimeSheetId = timeSheetId;
        return Result.Success();
    }

    //change Hours
    internal Result ChangeHours(decimal hours)
    {
        if (hours <= 0 || hours > 24)
            return Result.Failure(new Error("InvalidHours", "Hours must be greater than 0 and cannot exceed 24."));

        Hours = hours;
        return Result.Success();
    }

    internal Result Approve()
    {
        if (Status != TimeEntryStatus.Draft)
            return Result.Failure(new Error("TimeEntryNotDraft", "Only a draft time entry can be approved."));

        Status = TimeEntryStatus.Approved;
        return Result.Success();
    }

    internal Result Reject()
    {
        if (Status != TimeEntryStatus.Draft)
            return Result.Failure(new Error("TimeEntryNotDraft", "Only a draft time entry can be rejected."));

        Status = TimeEntryStatus.Rejected;
        return Result.Success();
    }

    internal Result Reopen()
    {
        if (Status != TimeEntryStatus.Rejected)
            return Result.Failure(new Error("TimeEntryNotRejected", "Only a rejected time entry can be reopened."));

        Status = TimeEntryStatus.Draft;
        return Result.Success();
    }

    internal Result Lock()
    {
        if (Status != TimeEntryStatus.Approved)
            return Result.Failure(new Error("TimeEntryNotApproved", "Only an approved time entry can be locked."));

        Status = TimeEntryStatus.Locked;
        return Result.Success();
    }
    //change date
    internal Result ChangeDate(DateTime date)
    {
        if (date == default)
            return Result.Failure(new Error("InvalidDate", "Date is required."));

        Date = date;
        return Result.Success();
    }
    //change comment
    internal Result ChangeComment(string? comment)
    {
        comment = comment?.Trim();
        if (comment?.Length > 1000)
            return Result.Failure(new Error("CommentTooLong", "Comment cannot exceed 1000 characters."));

        Comment = comment;
        return Result.Success();
    }

    internal Result ChangeTime(TimeSpan? startTime, TimeSpan? endTime)
    {
        var timeRangeResult = ValidateTimeRange(startTime, endTime);
        if (timeRangeResult.IsFailure)
            return timeRangeResult;

        StartTime = startTime;
        EndTime = endTime;
        return Result.Success();
    }

    private static Result ValidateTimeRange(TimeSpan? startTime, TimeSpan? endTime)
    {
        if (startTime.HasValue && (startTime.Value < TimeSpan.Zero || startTime.Value >= TimeSpan.FromDays(1)))
            return Result.Failure(new Error("InvalidStartTime", "Start time must be within the day."));

        if (endTime.HasValue && (endTime.Value < TimeSpan.Zero || endTime.Value >= TimeSpan.FromDays(1)))
            return Result.Failure(new Error("InvalidEndTime", "End time must be within the day."));

        if (startTime.HasValue && endTime.HasValue && endTime <= startTime)
            return Result.Failure(new Error("InvalidTimeRange", "End time must be greater than start time."));

        return Result.Success();
    }



}