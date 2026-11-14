using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record RejectionReason
{
    public string Value { get; }
    private RejectionReason(string value) => Value = value;

    public static Result<RejectionReason> Create(string? value) => string.IsNullOrWhiteSpace(value)
        ? Result<RejectionReason>.Failure(new Error("RejectReasonRequired", "A rejection reason is required."))
        : Result<RejectionReason>.Success(new RejectionReason(value.Trim()));

    public override string ToString() => Value;
}
