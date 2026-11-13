using Domain.Entities;

namespace Domain.Aggregate;

/*  Summary
 *  When an employee exports time entries, it will be saved in the database, so the employee can see previous exports
 */
public sealed class Export
{
    private readonly List<TimeEntryExport> _timeEntries = new();
    
    public Guid Id { get; set; }
    // Who exported
    public Guid ExportedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    //TimeEntries from date
    public DateOnly DateFrom { get; set; }
    //TimeEntries to date
    public DateOnly DateTo { get; set; }
    public IReadOnlyCollection<TimeEntryExport> TimeEntries => _timeEntries.AsReadOnly();
}
