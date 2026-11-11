using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class EmployeeSystemAccess
{
    public Guid EmployeeId { get; set; }
    public SystemPermission Permission { get; set; }
}
