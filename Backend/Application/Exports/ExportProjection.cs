using Domain.Aggregate;

namespace Application.Exports;

public static class ExportProjection
{
    public static ExportSummaryDto Summary(Export batch) => new(batch.Id.Value, batch.ExportedBy.Value, batch.CreatedAt,
        batch.DateFrom, batch.DateTo, batch.SelectedEmployeeId?.Value, batch.TimeEntries.Count, batch.TotalHours,
        batch.TimeEntriesClosedBy?.Value, batch.TimeEntriesClosedAtUtc);

    public static ExportDetailsDto Details(Export batch) => new(Summary(batch), batch.TimeEntries
        .OrderBy(item => item.TimeEntryId.Value).Select(item => new ExportEntryDto(item.TimeEntryId.Value, item.TimeEntryAuditId.Value)).ToArray());
}
