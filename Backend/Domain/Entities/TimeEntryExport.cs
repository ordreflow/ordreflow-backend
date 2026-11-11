namespace Domain.Entities;

/*  Summary
 *  list of which timeentries was included in which exports.
 *  by storing this, we can have a function to let payrollers press completed for an export,
 *  which sets the timeentries status to completed
 */
public sealed class TimeEntryExport
{
    public Guid ExportId { get; set; }
    public Guid TimeEntryId { get; set; }
}
