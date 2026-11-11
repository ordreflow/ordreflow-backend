using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Aggregate;

public sealed class ExportBatch
{
    private readonly List<TimeEntryId> _timeEntryIds = new();

    public ExportBatchId Id { get; private set; } = null!;
    public UserId CreatedByUserId { get; private set; } = null!;
    public ExportBatchStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public IReadOnlyCollection<TimeEntryId> TimeEntryIds => _timeEntryIds.AsReadOnly();

    private ExportBatch()
    {
    }

    private ExportBatch(UserId createdByUserId, IEnumerable<TimeEntryId> timeEntryIds)
    {
        Id = ExportBatchId.Create(Guid.NewGuid()).Value;
        CreatedByUserId = createdByUserId;
        _timeEntryIds.AddRange(timeEntryIds);
        Status = ExportBatchStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<ExportBatch> Create(
        UserId createdByUserId,
        IEnumerable<TimeEntryId> timeEntryIds)
    {
        if (createdByUserId is null)
            return Result<ExportBatch>.Failure(new Error("ExportCreatorRequired", "An export creator is required."));

        if (timeEntryIds is null)
            return Result<ExportBatch>.Failure(new Error("TimeEntriesRequired", "A fixed set of time entries is required."));

        var selection = timeEntryIds.ToArray();
        if (selection.Length == 0 || selection.Any(id => id is null))
            return Result<ExportBatch>.Failure(new Error("InvalidExportSelection", "An export batch must contain at least one valid time entry."));

        if (selection.Distinct().Count() != selection.Length)
            return Result<ExportBatch>.Failure(new Error("DuplicateExportSelection", "An export batch cannot contain duplicate time entries."));

        return Result<ExportBatch>.Success(new ExportBatch(createdByUserId, selection));
    }

    public Result Complete(DateTime completedAt)
    {
        if (Status != ExportBatchStatus.Pending)
            return Result.Failure(new Error("ExportBatchNotPending", "Only a pending export batch can be completed."));

        if (completedAt < CreatedAt)
            return Result.Failure(new Error("InvalidExportCompletionTime", "Completion time cannot be earlier than creation time."));

        Status = ExportBatchStatus.Completed;
        CompletedAt = completedAt;
        return Result.Success();
    }

    public Result Fail()
    {
        if (Status != ExportBatchStatus.Pending)
            return Result.Failure(new Error("ExportBatchNotPending", "Only a pending export batch can fail."));

        Status = ExportBatchStatus.Failed;
        return Result.Success();
    }

    public Result<IReadOnlyList<TimeEntryId>> DownloadAgain()
    {
        if (Status != ExportBatchStatus.Completed)
            return Result<IReadOnlyList<TimeEntryId>>.Failure(
                new Error("ExportBatchNotCompleted", "Only a completed export batch can be downloaded again."));

        return Result<IReadOnlyList<TimeEntryId>>.Success(_timeEntryIds.ToArray());
    }
}
