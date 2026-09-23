using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record OrderName
{
    public string Value { get; }

    private OrderName(string value) => Value = value;

    public static Result<OrderName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<OrderName>.Failure(new Error("CaseNameEmpty", "Order name cannot be empty."));

        value = value.Trim();
        return value.Length > 200
            ? Result<OrderName>.Failure(new Error("CaseNameTooLong", "Order name cannot exceed 200 characters."))
            : Result<OrderName>.Success(new OrderName(value));
    }

    public override string ToString() => Value;
}