using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record WorkDate
{
    private WorkDate(DateOnly value) => Value = value;

    public DateOnly Value { get; }

    public static Result<WorkDate> Create(DateOnly value) =>
        value == default
            ? Result<WorkDate>.Failure(new Error("WorkDateRequired", "A work date is required."))
            : Result<WorkDate>.Success(new WorkDate(value));
}

public sealed record WorkDuration
{
    private WorkDuration(double value) => Value = value;

    public double Value { get; }

    public static Result<WorkDuration> Create(double value) =>
        double.IsNaN(value) || double.IsInfinity(value) || value <= 0
            ? Result<WorkDuration>.Failure(new Error("InvalidDuration", "Duration must be a positive finite number."))
            : Result<WorkDuration>.Success(new WorkDuration(value));
}

public sealed record WorkDescription
{
    private const int MaximumLength = 1000;

    private WorkDescription(string value) => Value = value;

    public string Value { get; }

    public static Result<WorkDescription> Create(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized.Length > MaximumLength
            ? Result<WorkDescription>.Failure(
                new Error("DescriptionTooLong", $"The description cannot exceed {MaximumLength} characters."))
            : Result<WorkDescription>.Success(new WorkDescription(normalized));
    }
}

public sealed record RejectionReason
{
    private const int MaximumLength = 1000;

    private RejectionReason(string value) => Value = value;

    public string Value { get; }

    public static Result<RejectionReason> Create(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
            return Result<RejectionReason>.Failure(
                new Error("RejectReasonRequired", "A rejection reason is required."));
        if (normalized.Length > MaximumLength)
            return Result<RejectionReason>.Failure(
                new Error("RejectReasonTooLong", $"The rejection reason cannot exceed {MaximumLength} characters."));

        return Result<RejectionReason>.Success(new RejectionReason(normalized));
    }
}
