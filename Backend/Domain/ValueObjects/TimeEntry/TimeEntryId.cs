using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record TimeEntryId
{
    public Guid Value { get; }

    private TimeEntryId(Guid value) => Value = value;

    public static Result<TimeEntryId> Create(Guid value) => value == Guid.Empty
        ? Result<TimeEntryId>.Failure(new Error("TimeEntryIdEmpty", "Time entry id cannot be empty."))
        : Result<TimeEntryId>.Success(new TimeEntryId(value));
}