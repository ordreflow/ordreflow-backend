namespace Domain.Entities;

public sealed class TimeEntryExport
{
    public Guid ExportId { get; set; }
    public Guid TimeEntryId { get; set; }
}
