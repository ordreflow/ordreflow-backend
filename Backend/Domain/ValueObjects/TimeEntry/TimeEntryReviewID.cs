using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record TimeEntryReviewID
{
    public Guid Value { get; }

    private TimeEntryReviewID(Guid value) => Value = value;

    public static Result<TimeEntryReviewID> Create(Guid value) => value == Guid.Empty
        ? Result<TimeEntryReviewID>.Failure(new Error("TimeEntryIdEmpty", "Time entry id cannot be empty."))
        : Result<TimeEntryReviewID>.Success(new TimeEntryReviewID(value));
}