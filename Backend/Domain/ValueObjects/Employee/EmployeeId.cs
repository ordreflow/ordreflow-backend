using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record EmployeeId
{
    public Guid Value { get; }
    private EmployeeId(Guid value) => Value = value;

    public static Result<EmployeeId> Create(Guid value) => value == Guid.Empty
        ? Result<EmployeeId>.Failure(new Error("EmployeeIdEmpty", "Employee ID cannot be empty."))
        : Result<EmployeeId>.Success(new EmployeeId(value));

    public override string ToString() => Value.ToString();
}
