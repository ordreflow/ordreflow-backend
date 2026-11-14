using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

/// <summary>The duration of a single time entry, expressed in decimal hours.</summary>
public sealed record Duration
{
    public decimal Value { get; }

    private Duration(decimal value) => Value = value;

    public static Result<Duration> Create(decimal hours)
    {
        if (hours <= 0 || hours > 24)
            return Result<Duration>.Failure(new Error("InvalidDuration", "Duration must be greater than 0 and no more than 24 hours."));

        if (hours % 0.25m != 0m && hours is not (7.4m or 3.7m))
            return Result<Duration>.Failure(new Error("InvalidDurationIncrement",
                "Duration must be a quarter-hour increment, 7.4 hours, or 3.7 hours."));

        return Result<Duration>.Success(new Duration(hours));
    }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
