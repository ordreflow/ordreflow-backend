using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record CutoffDay
{
    public int Value { get; }
    private CutoffDay(int value) => Value = value;

    public static Result<CutoffDay> Create(int value) => value is < 1 or > 28
        ? Result<CutoffDay>.Failure(new Error("InvalidDayInMonth", "Day in month must be between 1 and 28."))
        : Result<CutoffDay>.Success(new CutoffDay(value));

    public override string ToString() => Value.ToString();
}
