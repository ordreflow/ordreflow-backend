using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record TaskId
{
    public Guid Value { get; }

    private TaskId(Guid value) => Value = value;

    public static Result<TaskId> Create(Guid value) => value == Guid.Empty
        ? Result<TaskId>.Failure(new Error("WorkIdEmpty", "Work item id cannot be empty."))
        : Result<TaskId>.Success(new TaskId(value));

    public override string ToString() => Value.ToString();
}