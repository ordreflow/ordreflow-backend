using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record TimeEntryAccessGrantId
{
    public Guid Value { get; }

    private TimeEntryAccessGrantId(Guid value) => Value = value;

    public static Result<TimeEntryAccessGrantId> Create(Guid value) => value == Guid.Empty
        ? Result<TimeEntryAccessGrantId>.Failure(new Error("AccessGrantIdEmpty", "Access grant id cannot be empty."))
        : Result<TimeEntryAccessGrantId>.Success(new TimeEntryAccessGrantId(value));

    public override string ToString() => Value.ToString();
}
