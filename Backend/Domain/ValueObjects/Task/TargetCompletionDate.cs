using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record TargetCompletionDate
{
    public DateOnly Value { get; }
    private TargetCompletionDate(DateOnly value) => Value = value;

    public static Result<TargetCompletionDate> Create(DateOnly value) => value == default
        ? Result<TargetCompletionDate>.Failure(new Error("TargetDateRequired", "A target completion date is required."))
        : Result<TargetCompletionDate>.Success(new TargetCompletionDate(value));

    public override string ToString() => Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
