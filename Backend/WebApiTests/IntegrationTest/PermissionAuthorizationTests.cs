using System.Security.Claims;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Authentication;

namespace WebApiTests.IntegrationTest;

public sealed class PermissionAuthorizationTests
{
    [Fact]
    public async Task Reviewer_policy_resolves_typed_employee_identity_without_role_claims()
    {
        var employee = Employee.Create("Reviewer", "reviewer@test.invalid", DateTime.UtcNow).Value;
        Assert.True(employee.GrantPermission(SystemPermission.CatagoryReviewer).IsSuccess);
        using var provider = Services(employee);
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var principal = Principal(employee.Id);

        Assert.True((await authorization.AuthorizeAsync(principal, null, AuthorizationPolicies.CatagoryReviewer)).Succeeded);

        employee.Status = EmployeeStatus.Deactivated;
        Assert.False((await authorization.AuthorizeAsync(principal, null, AuthorizationPolicies.CatagoryReviewer)).Succeeded);
    }

    [Fact]
    public async Task An_identity_role_claim_does_not_grant_reviewer_permission()
    {
        var employee = Employee.Create("Worker", "worker@test.invalid", DateTime.UtcNow).Value;
        using var provider = Services(employee);
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var principal = Principal(employee.Id);
        ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim(ClaimTypes.Role, "Admin"));

        Assert.False((await authorization.AuthorizeAsync(principal, null, AuthorizationPolicies.CatagoryReviewer)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(principal, null, AuthorizationPolicies.Employee)).Succeeded);
    }

    private static ClaimsPrincipal Principal(EmployeeId employeeId) =>
        new(new ClaimsIdentity(new[] { new Claim("sub", employeeId.Value.ToString()) }, "test"));

    private static ServiceProvider Services(Employee employee)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(AuthorizationPolicies.Configure);
        services.AddSingleton<IEmployeeAccessRepository>(new EmployeeRepository(employee));
        services.AddSingleton<IAuthorizationHandler, SystemPermissionAuthorizationHandler>();
        return services.BuildServiceProvider();
    }

    private sealed class EmployeeRepository(Employee employee) : IEmployeeAccessRepository
    {
        public Task<Employee?> GetWithAccessesAsync(EmployeeId employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Employee?>(employee.Id == employeeId ? employee : null);
    }
}
