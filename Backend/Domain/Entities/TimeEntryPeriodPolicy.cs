using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class TimeEntryPeriodPolicy
{
    public TimeEntryPeriodMode Mode { get; private set; }
    public int? MonthlyCutoffDay { get; private set; }
    public string TimeZoneId { get; private set; } = null!;

    private TimeEntryPeriodPolicy()
    {
    }

    private TimeEntryPeriodPolicy(
        TimeEntryPeriodMode mode,
        int? monthlyCutoffDay,
        string timeZoneId)
    {
        Mode = mode;
        MonthlyCutoffDay = monthlyCutoffDay;
        TimeZoneId = timeZoneId;
    }

    public static Result<TimeEntryPeriodPolicy> Create(
        TimeEntryPeriodMode mode,
        int? monthlyCutoffDay,
        string timeZoneId)
    {
        if (!Enum.IsDefined(mode))
            return Result<TimeEntryPeriodPolicy>.Failure(new Error("InvalidPeriodMode", "The time entry period mode is invalid."));

        if (string.IsNullOrWhiteSpace(timeZoneId))
            return Result<TimeEntryPeriodPolicy>.Failure(new Error("TimeZoneRequired", "A time zone is required."));

        if (mode == TimeEntryPeriodMode.MonthlyCutoff &&
            monthlyCutoffDay is not (>= 1 and <= 28))
            return Result<TimeEntryPeriodPolicy>.Failure(new Error("InvalidMonthlyCutoffDay", "Monthly cutoff day must be between 1 and 28."));

        if (mode == TimeEntryPeriodMode.Open && monthlyCutoffDay.HasValue)
            return Result<TimeEntryPeriodPolicy>.Failure(new Error("UnexpectedMonthlyCutoffDay", "An open period policy cannot have a monthly cutoff day."));

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return Result<TimeEntryPeriodPolicy>.Failure(new Error("InvalidTimeZone", "The selected time zone was not found."));
        }
        catch (InvalidTimeZoneException)
        {
            return Result<TimeEntryPeriodPolicy>.Failure(new Error("InvalidTimeZone", "The selected time zone is invalid."));
        }

        return Result<TimeEntryPeriodPolicy>.Success(new TimeEntryPeriodPolicy(
            mode,
            monthlyCutoffDay,
            timeZoneId));
    }

    public bool AllowsWorkDate(DateOnly workDate, DateTimeOffset now)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);

        if (workDate > localToday)
            return false;

        if (Mode == TimeEntryPeriodMode.Open)
            return true;

        var currentMonthStart = new DateOnly(localToday.Year, localToday.Month, 1);
        if (workDate >= currentMonthStart)
            return true;

        var previousMonthStart = currentMonthStart.AddMonths(-1);
        return workDate >= previousMonthStart &&
            localToday.Day <= MonthlyCutoffDay;
    }
}
