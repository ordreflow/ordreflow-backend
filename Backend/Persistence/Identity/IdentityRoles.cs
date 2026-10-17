namespace Persistence.Identity;

using Domain.ValueObjects;

public static class IdentityRoles
{
    public const string Employee = nameof(UserRole.Employee);
    public const string Manager = nameof(UserRole.Manager);
    public const string Admin = nameof(UserRole.Admin);
}
