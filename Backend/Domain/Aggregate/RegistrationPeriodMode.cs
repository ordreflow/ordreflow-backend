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
    public CutoffDay? DayInMonth { get; private set; }
    
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

        CutoffDay? cutoffDay = null;
        if (dayInMonth.HasValue)
        {
            var parsedDay = CutoffDay.Create(dayInMonth.Value);
            if (parsedDay.IsFailure)
                return Result<RegistrationPeriodMode>.Failure(parsedDay.Errors.ToArray());
            cutoffDay = parsedDay.Value;
        }

        return Result<RegistrationPeriodMode>.Success(new RegistrationPeriodMode
        {
            Id = RegistrationPeriodModeId.Create(Guid.NewGuid()).Value,
            AdminId = adminId,
            Interval = interval,
            DayInMonth = cutoffDay,
            CreatedDate = DateTime.UtcNow
        });
    }
}
