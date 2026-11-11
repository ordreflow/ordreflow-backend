namespace Domain.Entities;

public sealed class TaskAssinment
{
    public Guid TaskId { get; set; }
    public Guid AssignedTo { get; set; }
    public double ExpectedDuration { get; set; }
    public DateTime ExpectedDeadline { get; set; }
}
