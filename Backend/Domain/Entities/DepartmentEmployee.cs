using Domain.ValueObjects;

namespace Domain.Entities;

/*  Summary
 *  Employees are now assigned to departments instead of managers.
 *  Implemented this way so that departments and employees has Many-to-Many relation.
 *  In other words, employees can be assigned to multiple departments
 */

public sealed class DepartmentEmployee
{
    public DepartmentId DepartmentId { get; set; } = null!;
    public EmployeeId EmployeeId { get; set; } = null!;
}
