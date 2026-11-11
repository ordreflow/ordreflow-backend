namespace Domain.Entities;


// Access privilege and Tasks has many-to-many relations´
public sealed class AccessTask
{
    public Guid AccessId { get; set; }
    public Guid TaskId { get; set; }
}
