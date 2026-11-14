using System.Security.Claims;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;

namespace WebAPI.Authentication;

public sealed class SystemPermissionAuthorizationHandler(IEmployeeAccessRepository employees)
    : AuthorizationHandler<SystemPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, SystemPermissionRequirement requirement)
    {
        var subject = context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(subject, out var employeeId) || employeeId == Guid.Empty)
            return;

        var employee = await employees.GetWithAccessesAsync(employeeId);
        if (employee is not null && employee.HasPermission(SystemPermission.Employee) &&
            employee.HasPermission(requirement.Permission))
            context.Succeed(requirement);
    }
}
