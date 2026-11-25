using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record RejectionReason
{
    public string Value { get; }
    private RejectionReason(string value) => Value = value;

    public static Result<RejectionReason> Create(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
            return Result<RejectionReason>.Failure(new Error("RejectReasonRequired", "A rejection reason is required."));
        if (normalized.Length > 1000)
            return Result<RejectionReason>.Failure(new Error("RejectReasonTooLong", "The rejection reason cannot exceed 1000 characters."));

        return Result<RejectionReason>.Success(new RejectionReason(normalized));
    }

    public override string ToString() => Value;
}
