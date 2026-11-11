namespace Domain.Aggregate;

public sealed class AccessPrivilege
{
    public Guid Id { get; set; }
    public Guid IssuedBy { get; set; }
    public Guid IssuedTo { get; set; }
    public DateTime IssuedAtUtc { get; set; }
    public DateOnly EarliestWorkDate { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}

//Create, Edit, Close, GetAll, getIssuedTo, getIssuedBy