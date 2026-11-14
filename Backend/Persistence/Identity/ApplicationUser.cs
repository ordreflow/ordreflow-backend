namespace Persistence.Identity;

using Microsoft.AspNetCore.Identity;
using Domain.ValueObjects;

// Identity login account for an employee. System permissions live on EmployeeSystemAccess.
public class ApplicationUser : IdentityUser<Guid>
{
    public EmployeeId EmployeeId { get; set; } = null!;
}
