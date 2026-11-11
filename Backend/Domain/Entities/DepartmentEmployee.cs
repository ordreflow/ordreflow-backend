namespace Domain.Entities;

/*  Summary
 *  Employees are now assigned to departments instead of managers.
 *  Implemented this way so that departments and employees has Many-to-Many relation.
 *  In other words, employees can be assigned to multiple departments
 */

public sealed class DepartmentEmployee
{
    public Guid DepartmentId { get; set; }
    public Guid EmployeeId { get; set; }
}
