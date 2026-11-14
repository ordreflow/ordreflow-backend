using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Aggregate;

/*  Summary
 *  When an employee exports time entries, it will be saved in the database, so the employee can see previous exports
 */
public sealed class Export
{
    private readonly List<TimeEntryExport> _timeEntries = new();
    
    public ExportId Id { get; private set; } = null!;
    // Who exported
    public EmployeeId ExportedBy { get; private set; } = null!;
    public DateTime CreatedAt { get; set; }
    //TimeEntries from date
    public DateOnly DateFrom { get; private set; }
    //TimeEntries to date
    public DateOnly DateTo { get; private set; }
    public IReadOnlyCollection<TimeEntryExport> TimeEntries => _timeEntries.AsReadOnly();
}
