using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record CaseName
{
    public string Value { get; }

    private CaseName(string value) => Value = value;

    public static Result<CaseName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<CaseName>.Failure(new Error("CaseNameEmpty", "Case name cannot be empty."));

        value = value.Trim();
        return value.Length > 200
            ? Result<CaseName>.Failure(new Error("CaseNameTooLong", "Case name cannot exceed 200 characters."))
            : Result<CaseName>.Success(new CaseName(value));
    }

    public override string ToString() => Value;
}