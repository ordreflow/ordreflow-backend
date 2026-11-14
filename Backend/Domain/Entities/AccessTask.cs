using Domain.ValueObjects;

namespace Domain.Entities;


// Access privilege and Tasks has many-to-many relations´
public sealed class AccessTask
{
    public AccessPrivilegeId AccessId { get; set; } = null!;
    public TaskId TaskId { get; set; } = null!;
}
