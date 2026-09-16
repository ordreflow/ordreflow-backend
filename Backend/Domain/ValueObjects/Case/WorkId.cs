using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record WorkId
{
    public Guid Value { get; }

    private WorkId(Guid value) => Value = value;

    public static Result<WorkId> Create(Guid value) => value == Guid.Empty
        ? Result<WorkId>.Failure(new Error("WorkIdEmpty", "Work item id cannot be empty."))
        : Result<WorkId>.Success(new WorkId(value));

    public override string ToString() => Value.ToString();
}