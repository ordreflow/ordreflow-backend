using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record DepartmentId
{
    public Guid Value { get; }
    private DepartmentId(Guid value) => Value = value;

    public static Result<DepartmentId> Create(Guid value) => value == Guid.Empty
        ? Result<DepartmentId>.Failure(new Error("DepartmentIdEmpty", "Department ID cannot be empty."))
        : Result<DepartmentId>.Success(new DepartmentId(value));

    public override string ToString() => Value.ToString();
}
