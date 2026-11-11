using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record SystemAccessId
{
    public Guid Value { get; }

    private SystemAccessId(Guid value) => Value = value;

    public static Result<SystemAccessId> Create(Guid value) => value == Guid.Empty
        ? Result<SystemAccessId>.Failure(new Error("SystemAccessIdEmpty", "System access id cannot be empty."))
        : Result<SystemAccessId>.Success(new SystemAccessId(value));

    public override string ToString() => Value.ToString();
}
