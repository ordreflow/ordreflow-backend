using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

/// <summary>
/// A displayed job title such as "Electrician" or "Project lead" (FR-13). It is descriptive only and never grants
/// system access; access is controlled by <see cref="UserRole"/> and department management.
/// </summary>
public sealed record JobTitle
{
    public const int MaxLength = 100;

    public string Value { get; }

    private JobTitle(string value) => Value = value;

    public static Result<JobTitle> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<JobTitle>.Failure(new Error("JobTitleEmpty", "Job title cannot be empty."));

        value = value.Trim();
        return value.Length > MaxLength
            ? Result<JobTitle>.Failure(new Error("JobTitleTooLong", $"Job title cannot exceed {MaxLength} characters."))
            : Result<JobTitle>.Success(new JobTitle(value));
    }

    /// <summary>Returns no title for blank input, so a title can be cleared.</summary>
    public static Result<JobTitle?> CreateOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<JobTitle?>.Success(null);

        var result = Create(value);
        return result.IsSuccess
            ? Result<JobTitle?>.Success(result.Value)
            : Result<JobTitle?>.Failure(result.Errors.ToArray());
    }

    public override string ToString() => Value;
}
