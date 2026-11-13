using Domain.Entities;
using Domain.ValueObjects;
using Task = Domain.Entities.Task;

namespace Domain.Aggregate;

/*  Summary
 *  Catagory (previous order) is the different catagories the tasks can be put in,
 *  so the departments can group their tasks.
 *  Catagories could for an example be "Customer orders" or "Internal Meetings", which can include team weekly meetings, 1-on-1's etc. for plumbers department
 */
public sealed class Catagory
{
    private readonly List<Task> _tasks = new();
    
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid DepartmentId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public CatagoryStatus Status { get; set; }
    
    // set to true, if its a catagory that only the departments users should have access to enter time registrations
    public bool InternalOnly { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public IReadOnlyCollection<Task> Tasks => _tasks.AsReadOnly();
}

// Create, Close, Edit, SetInternalOnly
