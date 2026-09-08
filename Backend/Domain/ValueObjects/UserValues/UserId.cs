using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record UserId
{
    public Guid Value { get; }

    private UserId(Guid value) => Value = value;

    public static Result<UserId> Create(Guid value) => value == Guid.Empty
        ? Result<UserId>.Failure(new Error("UserIdEmpty", "User id cannot be empty."))
        : Result<UserId>.Success(new UserId(value));

    public override string ToString() => Value.ToString();
}