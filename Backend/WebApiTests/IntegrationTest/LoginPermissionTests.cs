using Application.Commands;
using Application.Handlers;
using Application.Interfaces;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace WebApiTests.IntegrationTest;

public sealed class LoginPermissionTests
{
    [Fact]
    public async Task Local_login_uses_the_employee_identity_and_baseline_permission()
    {
        var employee = Employee.Create("Employee", "employee@test.invalid", DateTime.UtcNow).Value;
        var tokens = new FakeTokenGenerator();
        var handler = new LoginHandler(new FakePasswordVerifier(employee.Id),
            new FakeEmployeeRepository(employee), tokens);

        var result = await handler.HandleAsync(new LoginCommand(employee.Email, "correct password"));

        Assert.True(result.IsSuccess);
        Assert.Same(employee, tokens.GeneratedFor);
        Assert.Equal("token", result.Value.AccessToken);
    }

    [Fact]
    public async Task Inactive_employee_cannot_receive_a_token()
    {
        var employee = Employee.Create("Employee", "employee@test.invalid", DateTime.UtcNow).Value;
        employee.Status = EmployeeStatus.Deactivated;
        var tokens = new FakeTokenGenerator();
        var handler = new LoginHandler(new FakePasswordVerifier(employee.Id),
            new FakeEmployeeRepository(employee), tokens);

        var result = await handler.HandleAsync(new LoginCommand(employee.Email, "correct password"));

        Assert.True(result.IsFailure);
        Assert.Equal("UserInactive", Assert.Single(result.Errors).Code);
        Assert.Null(tokens.GeneratedFor);
    }

    [Fact]
    public async Task Removing_baseline_permission_prevents_login()
    {
        var employee = new Employee
        {
            Id = Guid.NewGuid(), Name = "Employee", Email = "employee@test.invalid",
            Status = EmployeeStatus.Active, CreatedAt = DateTime.UtcNow
        };
        var tokens = new FakeTokenGenerator();
        var handler = new LoginHandler(new FakePasswordVerifier(employee.Id),
            new FakeEmployeeRepository(employee), tokens);

        var result = await handler.HandleAsync(new LoginCommand(employee.Email, "correct password"));

        Assert.True(result.IsFailure);
        Assert.Equal("AccessForbidden", Assert.Single(result.Errors).Code);
        Assert.Null(tokens.GeneratedFor);
    }

    [Fact]
    public async Task Invalid_credentials_do_not_query_employee_permissions()
    {
        var handler = new LoginHandler(new FakePasswordVerifier(null),
            new FakeEmployeeRepository(), new FakeTokenGenerator());

        var result = await handler.HandleAsync(new LoginCommand("employee@test.invalid", "incorrect"));

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidCredentials", Assert.Single(result.Errors).Code);
    }

    private sealed class FakePasswordVerifier(Guid? employeeId) : IPasswordVerifier
    {
        public Task<Guid?> VerifyAsync(string email, string password) => Task.FromResult(employeeId);
    }

    private sealed class FakeEmployeeRepository(Employee? employee = null) : IEmployeeAccessRepository
    {
        public Task<Employee?> GetWithAccessesAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            if (employee is null)
                throw new InvalidOperationException("Invalid credentials must not look up an employee.");
            return Task.FromResult(employee.Id == employeeId ? employee : null);
        }
    }

    private sealed class FakeTokenGenerator : IAccessTokenGenerator
    {
        public Employee? GeneratedFor { get; private set; }

        public AccessToken Generate(Employee employee)
        {
            GeneratedFor = employee;
            return new AccessToken("token", DateTime.UtcNow.AddMinutes(5));
        }
    }
}
