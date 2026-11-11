namespace Domain.Aggregate;

/*  Summary
 *  Departments are where each employee is grouped and the new owners of Catagories and tasks (which before was managers),
 *  Managers can be assigned to Departments, so they have the same access as they had before.
 *  Only difference is that Departments will now own and be linked to employees and catagories/tasks,
 *  and not directly
 *  to managers
 */
public sealed class Department
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

//Create, Edit, Delete, EditManagers, EditEmployees