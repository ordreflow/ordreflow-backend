using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record TimeSheetId
{
    public Guid Value { get; }

    private TimeSheetId(Guid value) => Value = value;

    public static Result<TimeSheetId> Create(Guid value) => value == Guid.Empty
        ? Result<TimeSheetId>.Failure(new Error("TimeSheetIdEmpty", "Timesheet id cannot be empty."))
        : Result<TimeSheetId>.Success(new TimeSheetId(value));

    public override string ToString() => Value.ToString();
}