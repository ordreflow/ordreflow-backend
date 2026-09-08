using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record CaseId
{
    public Guid Value { get; }

    private CaseId(Guid value) => Value = value;

    public static Result<CaseId> Create(Guid value) => value == Guid.Empty
        ? Result<CaseId>.Failure(new Error("CaseIdEmpty", "Case id cannot be empty."))
        : Result<CaseId>.Success(new CaseId(value));

    public override string ToString() => Value.ToString();
}