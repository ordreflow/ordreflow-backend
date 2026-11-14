using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application;
using Application.Commands;
using Application.Interfaces;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebAPI.Contracts.TimeEntries;

namespace WebApiTests.IntegrationTest;

[CollectionDefinition("Time registration HTTP", DisableParallelization = true)]
public sealed class TimeRegistrationHttpCollection { }

[Collection("Time registration HTTP")]
public sealed class TimeRegistrationHttpTests : IClassFixture<TimeRegistrationHttpTests.ApiFactory>
{
    private const string Path = "/api/time_entries";
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public TimeRegistrationHttpTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        _factory.Dispatcher.Reset();
    }

    [Fact]
    public async Task Anonymous_registration_is_unauthorized()
    {
        using var response = await _client.PostAsJsonAsync(Path, Request());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(_factory.Dispatcher.LastCommand);
    }

    [Fact]
    public async Task Missing_employee_permission_is_forbidden_before_dispatch()
    {
        var employee = new Employee
        {
            Id = EmployeeId.Create(Guid.NewGuid()).Value,
            Name = PersonName.Create("Worker").Value,
            Email = EmailAddress.Create("worker@test.invalid").Value,
            Status = EmployeeStatus.Active, CreatedAt = DateTime.UtcNow
        };
        _factory.Employees.Add(employee);

        using var response = await PostAsEmployeeAsync(employee, Request());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(_factory.Dispatcher.LastCommand);
    }

    [Fact]
    public async Task Registration_uses_authenticated_employee_and_existing_request_fields()
    {
        var employee = AddEmployee();
        var request = Request();

        using var response = await PostAsEmployeeAsync(employee, request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var command = Assert.IsType<CreateTimeEntryCommand>(_factory.Dispatcher.LastCommand);
        Assert.Equal(employee.Id.Value, command.EmployeeId);
        Assert.Equal(request.TaskId, command.TaskId);
        Assert.Equal(request.Date, command.Date);
        Assert.Equal(request.Hours, command.Hours);
        Assert.Equal(request.Comment, command.Comment);
    }

    [Fact]
    public async Task Assignment_denial_from_application_returns_forbidden()
    {
        var employee = AddEmployee();
        _factory.Dispatcher.NextResult = Result.Failure(
            new Error("TaskAssignmentForbidden", "This task is assigned to other employees."));

        using var response = await PostAsEmployeeAsync(employee, Request());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private Employee AddEmployee()
    {
        var employee = Employee.Create("Worker", $"{Guid.NewGuid():N}@test.invalid", DateTime.UtcNow).Value;
        _factory.Employees.Add(employee);
        return employee;
    }

    private static CreateTimeEntryRequest Request() =>
        new(Guid.NewGuid(), new DateTime(2026, 11, 14), 2.5m, null);

    private async Task<HttpResponseMessage> PostAsEmployeeAsync(Employee employee, CreateTimeEntryRequest request)
    {
        var token = _factory.Services.GetRequiredService<IAccessTokenGenerator>().Generate(employee).Value;
        using var message = new HttpRequestMessage(HttpMethod.Post, Path)
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(message);
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public FakeDispatcher Dispatcher { get; } = new();
        public FakeEmployeeRepository Employees { get; } = new();

        public ApiFactory()
        {
            Environment.SetEnvironmentVariable("JWT_SECRET", new string('x', 64));
            Environment.SetEnvironmentVariable("ADMIN_EMAIL", " ");
            Environment.SetEnvironmentVariable("ADMIN_PASSWORD", " ");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICommandDispatcher>();
                services.AddSingleton<ICommandDispatcher>(Dispatcher);
                services.RemoveAll<IEmployeeAccessRepository>();
                services.AddSingleton<IEmployeeAccessRepository>(Employees);
            });
        }
    }

    public sealed class FakeEmployeeRepository : IEmployeeAccessRepository
    {
        private readonly Dictionary<EmployeeId, Employee> _employees = new();

        public void Add(Employee employee) => _employees.Add(employee.Id, employee);

        public Task<Employee?> GetWithAccessesAsync(EmployeeId employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_employees.GetValueOrDefault(employeeId));
    }

    public sealed class FakeDispatcher : ICommandDispatcher
    {
        public object? LastCommand { get; private set; }
        public Result NextResult { get; set; } = Result.Success();

        public void Reset()
        {
            LastCommand = null;
            NextResult = Result.Success();
        }

        public Task<Result> DispatchAsync<TCommand>(TCommand command)
        {
            LastCommand = command;
            return Task.FromResult(NextResult);
        }
    }
}
