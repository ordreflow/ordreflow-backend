using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record PersonName
{
    public string Value { get; }

    private PersonName(string value) => Value = value;

    public static Result<PersonName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<PersonName>.Failure(new Error("PersonNameEmpty", "Person name cannot be empty."));

        value = value.Trim();
        return value.Length > 200
            ? Result<PersonName>.Failure(new Error("PersonNameTooLong", "Person name cannot exceed 200 characters."))
            : Result<PersonName>.Success(new PersonName(value));
    }

    public override string ToString() => Value;
}