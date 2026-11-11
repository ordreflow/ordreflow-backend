using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Aggregate;

public sealed class RegistrationPeriodMode
{
    public Guid Id { get; private set; }
    public Guid AdminId { get; private set; }
    public RegistrationInterval Interval { get; private set; }
    public int? DayInMonth { get; private set; }
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
