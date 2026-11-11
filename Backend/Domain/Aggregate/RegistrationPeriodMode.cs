using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Aggregate;

/*  Summary
 *  RegistrationPeriodMode defines the period for how long back in date employees can go to get write access
 *  to timeentries. 
 */

public sealed class RegistrationPeriodMode
{
    public Guid Id { get; private set; }
    
    // Who set the configuration
    public Guid AdminId { get; private set; }
    
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
        Guid adminId,
        RegistrationInterval interval,
        int? dayInMonth)
    {
        if (adminId == Guid.Empty)
            return Result<RegistrationPeriodMode>.Failure(new Error("AdminRequired", "An administrator is required."));

        if (!Enum.IsDefined(interval))
            return Result<RegistrationPeriodMode>.Failure(new Error("InvalidInterval", "The registration interval is invalid."));

        if (dayInMonth.HasValue && (dayInMonth.Value < 1 || dayInMonth.Value > 28))
            return Result<RegistrationPeriodMode>.Failure(new Error("InvalidDayInMonth", "Day in month must be between 1 and 28."));

        return Result<RegistrationPeriodMode>.Success(new RegistrationPeriodMode
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            Interval = interval,
            DayInMonth = dayInMonth,
            CreatedDate = DateTime.UtcNow
        });
    }
}
