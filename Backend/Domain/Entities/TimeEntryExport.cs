using Domain.ValueObjects;

namespace Domain.Entities;

// Pins the approved audit version in one saved batch. Overlapping batches are allowed.
public sealed class TimeEntryExport
{
    public ExportId ExportId { get; private set; } = null!;
    public TimeEntryId TimeEntryId { get; private set; } = null!;
    public TimeEntryAuditId TimeEntryAuditId { get; private set; } = null!;

    private TimeEntryExport() { }

    internal static TimeEntryExport Create(ExportId exportId, TimeEntryId entryId, TimeEntryAuditId auditId) =>
        new() { ExportId = exportId, TimeEntryId = entryId, TimeEntryAuditId = auditId };
}
