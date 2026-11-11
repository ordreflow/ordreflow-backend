using TaskStatus = Domain.ValueObjects.TaskStatus;

namespace Domain.Entities;

public sealed class Task
{
    public Guid Id { get; set; }
    public Guid CatagoryId { get; set; }
    public TaskStatus Status { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}
