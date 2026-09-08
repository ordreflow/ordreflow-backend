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

        if (startTime.HasValue && endTime.HasValue && endTime <= startTime)
            return Result<TimeEntry>.Failure(new Error("InvalidTimeRange", "End time must be greater than start time."));

        if (comment?.Length > 1000)
            return Result<TimeEntry>.Failure(new Error("CommentTooLong", "Comment cannot exceed 1000 characters."));

        return Result<TimeEntry>.Success(new TimeEntry(
            workId,
            date,
            hours,
            startTime,
            endTime,
            comment?.Trim()));
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
        if (comment?.Length > 1000)
            return Result.Failure(new Error("CommentTooLong", "Comment cannot exceed 1000 characters."));

        Comment = comment?.Trim();
        return Result.Success();
    }



}