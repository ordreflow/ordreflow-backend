using TaskStatus = Domain.ValueObjects.TaskStatus;

namespace Domain.Entities;

/*  Summary
 *  Tasks under Catagories, a task could be everyting employee does, from meetings to assignments,
 *  everything employees can register time to.
 */
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
