using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record TimeEntryReviewId
{
    public Guid Value { get; }
    private TimeEntryReviewId(Guid value) => Value = value;

    public static Result<TimeEntryReviewId> Create(Guid value) => value == Guid.Empty
        ? Result<TimeEntryReviewId>.Failure(new Error("TimeEntryReviewIdEmpty", "Review ID cannot be empty."))
        : Result<TimeEntryReviewId>.Success(new TimeEntryReviewId(value));

    public override string ToString() => Value.ToString();
}
