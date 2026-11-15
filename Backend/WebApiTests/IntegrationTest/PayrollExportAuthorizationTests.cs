using System.Security.Claims;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using WebAPI.Authentication;

namespace WebApiTests.IntegrationTest;

public sealed class PayrollExportAuthorizationTests
{
    [Theory]
    [InlineData(SystemPermission.Payroll, EmployeeStatus.Active, true)]
    [InlineData(SystemPermission.Administrator, EmployeeStatus.Active, true)]
    [InlineData(SystemPermission.Employee, EmployeeStatus.Active, false)]
    [InlineData(SystemPermission.CatagoryReviewer, EmployeeStatus.Active, false)]
    [InlineData(SystemPermission.DepartmentManager, EmployeeStatus.Active, false)]
    [InlineData(SystemPermission.Payroll, EmployeeStatus.Deactivated, false)]
    public async Task Export_policy_uses_current_system_grants_and_active_status(SystemPermission permission, EmployeeStatus status, bool permitted)
    {
        var employee = Employee.Create("User", "user@test.invalid", DateTime.UtcNow).Value;
        Assert.True(employee.GrantPermission(permission).IsSuccess);
        employee.Status = status;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(AuthorizationPolicies.Configure);
        services.AddSingleton<IEmployeeAccessRepository>(new Employees(employee));
        services.AddScoped<IAuthorizationHandler, SystemPermissionAuthorizationHandler>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", employee.Id.Value.ToString())], "test"));

        var result = await authorization.AuthorizeAsync(principal, null, AuthorizationPolicies.Export);
        var anonymous = await authorization.AuthorizeAsync(new ClaimsPrincipal(), null, AuthorizationPolicies.Export);

        Assert.Equal(permitted, result.Succeeded);
        Assert.False(anonymous.Succeeded);
    }

    private sealed class Employees(Employee employee) : IEmployeeAccessRepository
    {
        public Task<Employee?> GetWithAccessesAsync(EmployeeId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Employee?>(id == employee.Id ? employee : null);
    }
}
