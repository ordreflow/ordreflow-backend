using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record WorkDate
{
    public DateOnly Value { get; }
    private WorkDate(DateOnly value) => Value = value;

    public static Result<WorkDate> Create(DateOnly value) => value == default
        ? Result<WorkDate>.Failure(new Error("WorkDateRequired", "A work date is required."))
        : Result<WorkDate>.Success(new WorkDate(value));

    public override string ToString() => Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
