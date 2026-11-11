using Domain.ValueObjects;

namespace Domain.Aggregate;

public sealed class Catagory
{
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid DepartmentId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public CatagoryStatus Status { get; set; }
    public bool InternalOnly { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

// Create, Close, Edit, SetInternalOnly
