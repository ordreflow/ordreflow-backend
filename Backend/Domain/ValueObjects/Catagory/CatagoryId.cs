using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record CatagoryId
{
    public Guid Value { get; }
    private CatagoryId(Guid value) => Value = value;

    public static Result<CatagoryId> Create(Guid value) => value == Guid.Empty
        ? Result<CatagoryId>.Failure(new Error("CatagoryIdEmpty", "Category ID cannot be empty."))
        : Result<CatagoryId>.Success(new CatagoryId(value));

    public override string ToString() => Value.ToString();
}
