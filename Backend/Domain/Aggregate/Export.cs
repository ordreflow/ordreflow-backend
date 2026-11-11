namespace Domain.Aggregate;

public sealed class Export
{
    public Guid Id { get; set; }
    public Guid ExportedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
}
