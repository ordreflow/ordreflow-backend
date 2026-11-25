using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

/// <summary>Expected work in decimal hours; a plan may span more than one day.</summary>
public sealed record PlannedDuration
{
    public decimal Value { get; }
    private PlannedDuration(decimal value) => Value = value;

    public static Result<PlannedDuration> Create(decimal value) => value <= 0
        ? Result<PlannedDuration>.Failure(new Error("InvalidPlannedDuration", "Expected duration must be positive."))
        : Result<PlannedDuration>.Success(new PlannedDuration(value));

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
