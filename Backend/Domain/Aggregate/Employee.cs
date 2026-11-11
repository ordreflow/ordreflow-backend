using Domain.ValueObjects;

namespace Domain.Aggregate;

/*  Summary
 *  Employees (previously Users) is the users of the time registration.
 */
public sealed class Employee
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    
    // Status for employee, if its active or deactivated.
    public EmployeeStatus Status { get; set; }
    
    // Nothing functional, just an optional title that can be put in to see the role of the employee if needed
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Create, Edit, 