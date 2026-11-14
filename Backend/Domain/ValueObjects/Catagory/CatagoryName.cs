using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record CatagoryName
{
    public string Value { get; }
    private CatagoryName(string value) => Value = value;

    public static Result<CatagoryName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<CatagoryName>.Failure(new Error("CatagoryNameRequired", "Category name is required."));
        value = value.Trim();
        return value.Length > 200
            ? Result<CatagoryName>.Failure(new Error("CatagoryNameTooLong", "Category name cannot exceed 200 characters."))
            : Result<CatagoryName>.Success(new CatagoryName(value));
    }

    public override string ToString() => Value;
}
