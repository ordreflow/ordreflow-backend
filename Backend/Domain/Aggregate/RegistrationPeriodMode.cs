using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Aggregate;

/*  Summary
 *  RegistrationPeriodMode defines the period for how long back in date employees can go to get write access
 *  to timeentries. 
 */

public sealed class RegistrationPeriodMode
{
    public RegistrationPeriodModeId Id { get; private set; } = null!;
    
    // Who set the configuration
    public EmployeeId AdminId { get; private set; } = null!;
    
    // What is the setting set to, check the different values in ValueObjects.RegistrationInterval.cs
    public RegistrationInterval Interval { get; private set; }
    
    // If DayInMonth is selected in Interval, which day is it. it can be a day between 1st and 28th in a month.
    // If for example the day was 18, then an Interval would the from 18th to 17th next month
    public int? DayInMonth { get; private set; }
    
    // When was the configuration made
    public DateTime CreatedDate { get; private set; }

    private RegistrationPeriodMode()
    {
    }

    public static Result<RegistrationPeriodMode> Create(
        EmployeeId adminId,
        RegistrationInterval interval,
        int? dayInMonth)
    {
        if (adminId is null)
            return Result<RegistrationPeriodMode>.Failure(new Error("AdminRequired", "An administrator is required."));

        if (!Enum.IsDefined(interval))
            return Result<RegistrationPeriodMode>.Failure(new Error("InvalidInterval", "The registration interval is invalid."));

        if (dayInMonth.HasValue && (dayInMonth.Value < 1 || dayInMonth.Value > 28))
            return Result<RegistrationPeriodMode>.Failure(new Error("InvalidDayInMonth", "Day in month must be between 1 and 28."));

        return Result<RegistrationPeriodMode>.Success(new RegistrationPeriodMode
        {
            Id = RegistrationPeriodModeId.New(),
            AdminId = adminId,
            Interval = interval,
            DayInMonth = dayInMonth,
            CreatedDate = DateTime.UtcNow
        });
    }

    public Result<WorkDatePeriod> GetPermittedWorkDates(DateOnly today)
    {
        if (today == default)
            return Result<WorkDatePeriod>.Failure(
                new Error("CurrentDateRequired", "The current date is required."));

        DateOnly from;
        DateOnly through;
        switch (Interval)
        {
            case RegistrationInterval.Daily:
                from = through = today;
                break;
            case RegistrationInterval.Weekly:
                from = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
                through = from.AddDays(6);
                break;
            case RegistrationInterval.Monthy:
                from = new DateOnly(today.Year, today.Month, 1);
                through = from.AddMonths(1).AddDays(-1);
                break;
            case RegistrationInterval.DayInMonth when DayInMonth is >= 1 and <= 28:
                var thisMonthAnchor = new DateOnly(today.Year, today.Month, DayInMonth.Value);
                from = today >= thisMonthAnchor
                    ? thisMonthAnchor
                    : thisMonthAnchor.AddMonths(-1);
                through = from.AddMonths(1).AddDays(-1);
                break;
            case RegistrationInterval.DayInMonth:
                return Result<WorkDatePeriod>.Failure(
                    new Error("InvalidRegistrationPeriod", "A valid day of month is required for this interval."));
            default:
                return Result<WorkDatePeriod>.Failure(
                    new Error("InvalidInterval", "The registration interval is invalid."));
        }

        var firstDate = WorkDate.Create(from);
        var lastDate = WorkDate.Create(through);
        if (firstDate.IsFailure || lastDate.IsFailure)
            return Result<WorkDatePeriod>.Failure(
                (firstDate.IsFailure ? firstDate.Errors : lastDate.Errors).ToArray());

        return WorkDatePeriod.Create(firstDate.Value, lastDate.Value);
    }
}
