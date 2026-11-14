using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record TimeEntryAuditId
{
    public Guid Value { get; }
    private TimeEntryAuditId(Guid value) => Value = value;

    public static Result<TimeEntryAuditId> Create(Guid value) => value == Guid.Empty
        ? Result<TimeEntryAuditId>.Failure(new Error("TimeEntryAuditIdEmpty", "Audit ID cannot be empty."))
        : Result<TimeEntryAuditId>.Success(new TimeEntryAuditId(value));

    public override string ToString() => Value.ToString();
}
