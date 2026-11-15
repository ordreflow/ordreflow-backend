using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.ValueObjects;

namespace Domain.Services;

public sealed class ExportClosureDomainService
{
    public Result Close(Export batch, IReadOnlyCollection<TimeEntry> entries, EmployeeId actor, DateTime atUtc)
    {
        if (batch is null) return Result.Failure(new Error("ExportRequired", "A saved export is required."));
        var validBatch = batch.CanRecordClosure(actor, atUtc);
        if (validBatch.IsFailure || batch.TimeEntriesClosedAtUtc is not null) return validBatch;
        if (entries is null || entries.Count != batch.TimeEntries.Count || entries.Select(item => item.Id).Distinct().Count() != entries.Count)
            return Conflict();
        var byId = entries.ToDictionary(item => item.Id);
        if (batch.TimeEntries.Any(item => !byId.ContainsKey(item.TimeEntryId))) return Conflict();
        foreach (var item in batch.TimeEntries)
        {
            var valid = byId[item.TimeEntryId].CanCloseForExport(item.TimeEntryAuditId, atUtc);
            if (valid.IsFailure) return valid;
        }
        foreach (var item in batch.TimeEntries)
        {
            var result = byId[item.TimeEntryId].CloseForExport(item.TimeEntryAuditId, actor, atUtc);
            if (result.IsFailure) return result;
        }
        return batch.RecordTimeEntriesClosed(actor, atUtc);
    }

    private static Result Conflict() => Result.Failure(new Error("ExportSelectionConflict", "The saved export selection is incomplete or duplicated."));
}
