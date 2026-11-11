namespace Persistence.Identity;

using Microsoft.AspNetCore.Identity;

// Identity login account for an employee. System permissions live on EmployeeSystemAccess.
public class ApplicationUser : IdentityUser<Guid>
{
    public Guid EmployeeId { get; set; }
}
