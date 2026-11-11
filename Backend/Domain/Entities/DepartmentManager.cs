namespace Domain.Entities;

/*  Summary
 *  defines which employees that is assigned as department manager. department manager has same privileges as,
 *  the managers before, they can view the employees, crud operations to catagory, and tasks, review if want to.
 *  Only difference is that its all owned by departments now and not an manager directly
 */
public sealed class DepartmentManager
{
    public Guid DepartmentId { get; set; }
    // Employee that is assigned as Department manager
    public Guid ManagerId { get; set; }
}
