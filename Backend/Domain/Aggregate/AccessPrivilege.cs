using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Aggregate;

/* Summary
 * When an Employee needs to access timeentries outside the given registration period,
 * an admin can give an access privilege to that employee in a given time
 *
 * AccessTask.cs defines which tasks the access privilege is scoped in
 * */
public sealed class AccessPrivilege
{
    private readonly List<AccessTask> _tasks = new();
    
    public AccessPrivilegeId Id { get; set; } = null!;
    //Who gave the access
    public EmployeeId IssuedBy { get; set; } = null!;
    //who got access
    public EmployeeId IssuedTo { get; set; } = null!;
    //When was the access issued
    public DateTime IssuedAtUtc { get; set; }
    
    //How long back in date the employee can go to edit timeregistrations
    public DateOnly EarliestWorkDate { get; private set; }
    
    //When the accessprivilege expires
    public DateTime ExpiresAtUtc { get; set; }
    
    public IReadOnlyCollection<AccessTask> Tasks => _tasks.AsReadOnly();
}

//Create, Edit, Close, GetAll, getIssuedTo, getIssuedBy
