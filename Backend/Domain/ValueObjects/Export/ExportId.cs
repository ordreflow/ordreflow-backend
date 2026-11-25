using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record ExportId
{
    public Guid Value { get; }
    private ExportId(Guid value) => Value = value;

    public static Result<ExportId> Create(Guid value) => value == Guid.Empty
        ? Result<ExportId>.Failure(new Error("ExportIdEmpty", "Export ID cannot be empty."))
        : Result<ExportId>.Success(new ExportId(value));

    public override string ToString() => Value.ToString();
}
