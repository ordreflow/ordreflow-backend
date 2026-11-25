using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record AccessPrivilegeId
{
    public Guid Value { get; }
    private AccessPrivilegeId(Guid value) => Value = value;

    public static Result<AccessPrivilegeId> Create(Guid value) => value == Guid.Empty
        ? Result<AccessPrivilegeId>.Failure(new Error("AccessPrivilegeIdEmpty", "Access privilege ID cannot be empty."))
        : Result<AccessPrivilegeId>.Success(new AccessPrivilegeId(value));

    public override string ToString() => Value.ToString();
}
