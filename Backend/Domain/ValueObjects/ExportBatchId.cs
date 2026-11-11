using Core.Tools.OperationResult;

namespace Domain.ValueObjects;

public sealed record ExportBatchId
{
    public Guid Value { get; }

    private ExportBatchId(Guid value) => Value = value;

    public static Result<ExportBatchId> Create(Guid value) => value == Guid.Empty
        ? Result<ExportBatchId>.Failure(new Error("ExportBatchIdEmpty", "Export batch id cannot be empty."))
        : Result<ExportBatchId>.Success(new ExportBatchId(value));

    public override string ToString() => Value.ToString();
}
