using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public  record CompanyName
{
    public string Value { get; }

    private CompanyName(string value)
    {
        Value = value;
    }

    public  static Result<CompanyName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<CompanyName>.Failure(new Error("CompanyNameCannotBeEmpty", "Company name cannot be empty."));
        }

        value = value.Trim();

        if (value.Length > 100)
        {
            return Result<CompanyName>.Failure(new Error("CompanyNameTooLong", "Company name cannot exceed 100 characters."));
        }

        return Result<CompanyName>.Success(new CompanyName(value));
    }

    public override string ToString()
    {
        return Value;
    }
}