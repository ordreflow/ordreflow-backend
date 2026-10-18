using Microsoft.AspNetCore.Authorization;
using Persistence.Identity;

namespace WebAPI.Authentication;

// Coarse role gates on the endpoints. The domain still checks scope, e.g. a manager's own employees.
public static class AuthorizationPolicies
{
    public const string AnyRole = nameof(AnyRole);
    public const string ManagerOrAdmin = nameof(ManagerOrAdmin);
    public const string AdminOnly = nameof(AdminOnly);

    public static void Configure(AuthorizationOptions options)
    {
        var anyRole = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole(IdentityRoles.Employee, IdentityRoles.Manager, IdentityRoles.Admin)
            .Build();

        // A plain [Authorize] requires a valid token with one of the three roles.
        options.DefaultPolicy = anyRole;
        options.AddPolicy(AnyRole, anyRole);

        options.AddPolicy(ManagerOrAdmin, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(IdentityRoles.Manager, IdentityRoles.Admin));

        options.AddPolicy(AdminOnly, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(IdentityRoles.Admin));
    }
}
