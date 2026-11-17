using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record DepartmentName
{
    public const int MaxLength = 100;

    public string Value { get; }

    private DepartmentName(string value) => Value = value;

    public static Result<DepartmentName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<DepartmentName>.Failure(new Error("DepartmentNameRequired", "Department name is required."));

        value = value.Trim();
        return value.Length > MaxLength
            ? Result<DepartmentName>.Failure(new Error("DepartmentNameTooLong", $"Department name cannot exceed {MaxLength} characters."))
            : Result<DepartmentName>.Success(new DepartmentName(value));
    }

    public override string ToString() => Value;
}
