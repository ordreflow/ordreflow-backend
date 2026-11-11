using Domain.ValueObjects;

namespace Domain.Aggregate;

public sealed class Employee
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public EmployeeStatus Status { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Create, Edit, 