namespace Persistence.Identity;

using Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;

// Login account for a domain user. Name, role and status stay on Domain.Aggregate.User.
public class ApplicationUser : IdentityUser<Guid>
{
    public UserId UserId { get; set; } = null!;
}
