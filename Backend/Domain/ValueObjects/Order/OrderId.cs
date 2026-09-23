using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record OrderId
{
    public Guid Value { get; }

    private OrderId(Guid value) => Value = value;

    public static Result<OrderId> Create(Guid value) => value == Guid.Empty
        ? Result<OrderId>.Failure(new Error("CaseIdEmpty", "Order id cannot be empty."))
        : Result<OrderId>.Success(new OrderId(value));

    public override string ToString() => Value.ToString();
}