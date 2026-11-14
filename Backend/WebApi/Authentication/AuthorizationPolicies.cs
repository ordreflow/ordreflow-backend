using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;

namespace WebAPI.Authentication;

public static class AuthorizationPolicies
{
    public const string Employee = nameof(Employee);
    public const string Administrator = nameof(Administrator);
    public const string Payroll = nameof(Payroll);
    public const string DepartmentManager = nameof(DepartmentManager);
    public const string CatagoryReviewer = nameof(CatagoryReviewer);

    // Legacy M2 endpoints are unavailable until their M3 permission and scope rules are implemented.
    public const string UnavailableUntilRefactored = nameof(UnavailableUntilRefactored);

    public static void Configure(AuthorizationOptions options)
    {
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        AddPermissionPolicy(options, Employee, SystemPermission.Employee);
        AddPermissionPolicy(options, Administrator, SystemPermission.Administrator);
        AddPermissionPolicy(options, Payroll, SystemPermission.Payroll);
        AddPermissionPolicy(options, DepartmentManager, SystemPermission.DepartmentManager);
        AddPermissionPolicy(options, CatagoryReviewer, SystemPermission.CatagoryReviewer);

        options.AddPolicy(UnavailableUntilRefactored, policy => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(_ => false));
    }

    private static void AddPermissionPolicy(
        AuthorizationOptions options, string name, SystemPermission permission) =>
        options.AddPolicy(name, policy => policy
            .RequireAuthenticatedUser()
            .AddRequirements(new SystemPermissionRequirement(permission)));
}
