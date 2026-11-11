using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record WorkAssignmentId
{
    public Guid Value { get; }

    private WorkAssignmentId(Guid value) => Value = value;

    public static Result<WorkAssignmentId> Create(Guid value) => value == Guid.Empty
        ? Result<WorkAssignmentId>.Failure(new Error("WorkAssignmentIdEmpty", "Work assignment id cannot be empty."))
        : Result<WorkAssignmentId>.Success(new WorkAssignmentId(value));

    public override string ToString() => Value.ToString();
}
