using Domain.Entities;
using Domain.ValueObjects;
using Core.Tools.OperationResult;

namespace Domain.Aggregate;

// A fixed payroll selection. Creating/downloading it never closes its entries.
public sealed class Export
{
    private readonly List<TimeEntryExport> _timeEntries = new();
    
    public ExportId Id { get; private set; } = null!;
    // Who exported
    public EmployeeId ExportedBy { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    //TimeEntries from date
    public DateOnly DateFrom { get; private set; }
    //TimeEntries to date
    public DateOnly DateTo { get; private set; }
    public EmployeeId? SelectedEmployeeId { get; private set; }
    public EmployeeId? TimeEntriesClosedBy { get; private set; }
    public DateTime? TimeEntriesClosedAtUtc { get; private set; }
    public long Revision { get; private set; }
    public decimal TotalHours { get; private set; }
    private byte[] _csvContent = [];
    public byte[] CsvContent => _csvContent.ToArray();
    public IReadOnlyCollection<TimeEntryExport> TimeEntries => _timeEntries.AsReadOnly();

    private Export() { }

    public static Result<Export> Create(ExportId id, EmployeeId actor, DateOnly from, DateOnly to,
        EmployeeId? selectedEmployee, IReadOnlyCollection<TimeEntry> entries, byte[] csv, DateTime createdAtUtc)
    {
        if (id is null || actor is null)
            return Result<Export>.Failure(new Error("ExportIdentityRequired", "The export and creator IDs are required."));
        if (from == default || to < from)
            return Result<Export>.Failure(new Error("InvalidDateRange", "A valid inclusive date range is required."));
        if (createdAtUtc == default || createdAtUtc.Kind != DateTimeKind.Utc)
            return Result<Export>.Failure(new Error("ExportTimeUtcRequired", "The creation time must be UTC."));
        if (entries is null || entries.Count == 0)
            return Result<Export>.Failure(new Error("NoApprovedEntries", "No approved entries match the selection."));
        if (csv is null || csv.Length == 0)
            return Result<Export>.Failure(new Error("ExportContentRequired", "The export file is required."));
        if (entries.Select(entry => entry.Id).Distinct().Count() != entries.Count)
            return Result<Export>.Failure(new Error("DuplicateExportEntry", "An entry may only appear once in a batch."));

        var batch = new Export
        {
            Id = id, ExportedBy = actor, DateFrom = from, DateTo = to,
            SelectedEmployeeId = selectedEmployee, CreatedAt = createdAtUtc, _csvContent = csv.ToArray(), Revision = 1
        };
        foreach (var entry in entries)
        {
            var audit = entry.GetCurrentAudit();
            if (audit is null || audit.Status != TimeEntryStatus.Approved || audit.WorkDate.Value < from ||
                audit.WorkDate.Value > to || (selectedEmployee is not null && entry.EmployeeId != selectedEmployee))
                return Result<Export>.Failure(new Error("ExportSelectionConflict", "The selection contains an ineligible entry."));
            batch._timeEntries.Add(TimeEntryExport.Create(id, entry.Id, audit.Id));
            batch.TotalHours += audit.Duration.Value;
        }
        return Result<Export>.Success(batch);
    }

    public bool MatchesRequest(EmployeeId actor, DateOnly from, DateOnly to, EmployeeId? employee) =>
        ExportedBy == actor && DateFrom == from && DateTo == to && SelectedEmployeeId == employee;

    public Result CanRecordClosure(EmployeeId actor, DateTime closedAtUtc)
    {
        if (TimeEntriesClosedAtUtc is not null)
            return Result.Success();
        if (actor is null || closedAtUtc.Kind != DateTimeKind.Utc || closedAtUtc < CreatedAt)
            return Result.Failure(new Error("ExportClosureInvalid", "A closing actor and a valid UTC timestamp are required."));
        return Result.Success();
    }

    internal Result RecordTimeEntriesClosed(EmployeeId actor, DateTime closedAtUtc)
    {
        var valid = CanRecordClosure(actor, closedAtUtc);
        if (valid.IsFailure || TimeEntriesClosedAtUtc is not null) return valid;
        TimeEntriesClosedBy = actor;
        TimeEntriesClosedAtUtc = closedAtUtc;
        Revision++;
        return Result.Success();
    }
}
