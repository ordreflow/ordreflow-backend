using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record TimeEntryDescription
{
    public string Value { get; }
    private TimeEntryDescription(string value) => Value = value;

    public static Result<TimeEntryDescription> Create(string? value)
    {
        value = value?.Trim() ?? string.Empty;
        return value.Length > 1000
            ? Result<TimeEntryDescription>.Failure(new Error("DescriptionTooLong", "Description cannot exceed 1000 characters."))
            : Result<TimeEntryDescription>.Success(new TimeEntryDescription(value));
    }

    public override string ToString() => Value;
}
