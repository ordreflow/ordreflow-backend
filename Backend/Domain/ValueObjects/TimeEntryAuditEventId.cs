using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record TimeEntryAuditEventId
{
    public Guid Value { get; }

    private TimeEntryAuditEventId(Guid value) => Value = value;

    public static Result<TimeEntryAuditEventId> Create(Guid value) => value == Guid.Empty
        ? Result<TimeEntryAuditEventId>.Failure(new Error("AuditEventIdEmpty", "Audit event id cannot be empty."))
        : Result<TimeEntryAuditEventId>.Success(new TimeEntryAuditEventId(value));

    public override string ToString() => Value.ToString();
}
