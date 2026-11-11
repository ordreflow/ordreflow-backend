namespace Domain.Aggregate;

/* Summary
 * When an Employee needs to access timeentries outside the given registration period,
 * an admin can give an access privilege to that employee in a given time
 *
 * AccessTask.cs defines which tasks the access privilege is scoped in
 * */
public sealed class AccessPrivilege
{
    public Guid Id { get; set; }
    
    //Who gave the access
    public Guid IssuedBy { get; set; }
    //who got access
    public Guid IssuedTo { get; set; }
    //When was the access issued
    public DateTime IssuedAtUtc { get; set; }
    
    //How long back in date the employee can go to edit timeregistrations
    public DateOnly EarliestWorkDate { get; set; }
    
    //When the accessprivilege expires
    public DateTime ExpiresAtUtc { get; set; }
}

//Create, Edit, Close, GetAll, getIssuedTo, getIssuedBy