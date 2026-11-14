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

[CollectionDefinition("Reviewer decisions HTTP", DisableParallelization = true)]
public sealed class ReviewerDecisionHttpCollection { }

[Collection("Reviewer decisions HTTP")]
public sealed class ReviewerDecisionHttpTests : IClassFixture<ReviewerDecisionHttpTests.ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ReviewerDecisionHttpTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        _factory.Dispatcher.Reset();
    }

    [Fact]
    public async Task Anonymous_review_is_unauthorized()
    {
        using var response = await _client.PostAsync($"/api/time_entries/{Guid.NewGuid()}/approve", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(_factory.Dispatcher.LastCommand);
    }

    [Fact]
    public async Task Employee_without_category_reviewer_permission_is_forbidden_before_dispatch()
    {
        var employee = AddEmployee();

        using var response = await SendAsync(employee, $"/api/time_entries/{Guid.NewGuid()}/reject",
            new RejectTimeEntryRequest("Reason"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(_factory.Dispatcher.LastCommand);
    }

    [Fact]
    public async Task Reviewer_can_dispatch_approve_and_reject_without_identity_roles()
    {
        var reviewer = AddEmployee(reviewer: true);
        var approvedId = Guid.NewGuid();
        var rejectedId = Guid.NewGuid();

        using var approved = await SendAsync(reviewer, $"/api/time_entries/{approvedId}/approve");
        Assert.Equal(HttpStatusCode.NoContent, approved.StatusCode);
        var approveCommand = Assert.IsType<ApproveTimeEntryCommand>(_factory.Dispatcher.LastCommand);
        Assert.Equal(approvedId, approveCommand.TimeEntryId);
        Assert.Equal(reviewer.Id.Value, approveCommand.ReviewerId);

        using var rejected = await SendAsync(reviewer, $"/api/time_entries/{rejectedId}/reject",
            new RejectTimeEntryRequest("Incorrect hours"));
        Assert.Equal(HttpStatusCode.NoContent, rejected.StatusCode);
        var rejectCommand = Assert.IsType<RejectTimeEntryCommand>(_factory.Dispatcher.LastCommand);
        Assert.Equal(rejectedId, rejectCommand.TimeEntryId);
        Assert.Equal("Incorrect hours", rejectCommand.Reason);
    }

    [Fact]
    public async Task Existing_accept_and_return_routes_remain_compatible()
    {
        var reviewer = AddEmployee(reviewer: true);

        using var accepted = await SendAsync(reviewer, $"/api/time_entries/{Guid.NewGuid()}/accept");
        Assert.Equal(HttpStatusCode.NoContent, accepted.StatusCode);
        Assert.IsType<ApproveTimeEntryCommand>(_factory.Dispatcher.LastCommand);

        using var returned = await SendAsync(reviewer, $"/api/time_entries/{Guid.NewGuid()}/return",
            new ReturnTimeEntryRequest("Correct this"));
        Assert.Equal(HttpStatusCode.NoContent, returned.StatusCode);
        Assert.IsType<RejectTimeEntryCommand>(_factory.Dispatcher.LastCommand);
    }

    [Fact]
    public async Task Concurrent_change_returns_conflict_instead_of_success()
    {
        var reviewer = AddEmployee(reviewer: true);
        _factory.Dispatcher.NextResult = Result.Failure(new Error("TimeEntryConflict", "Refresh and retry."));

        using var response = await SendAsync(reviewer, $"/api/time_entries/{Guid.NewGuid()}/approve");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Review_endpoint_does_not_finalize_an_entry()
    {
        var reviewer = AddEmployee(reviewer: true);

        using var response = await SendAsync(reviewer, $"/api/time_entries/{Guid.NewGuid()}/review",
            new ReviewTimeEntryRequest("finalize", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(_factory.Dispatcher.LastCommand);
    }

    private Employee AddEmployee(bool reviewer = false)
    {
        var employee = Employee.Create("Reviewer", $"{Guid.NewGuid():N}@test.invalid", DateTime.UtcNow).Value;
        if (reviewer)
            Assert.True(employee.GrantPermission(SystemPermission.CatagoryReviewer).IsSuccess);
        _factory.Employees.Add(employee);
        return employee;
    }

    private async Task<HttpResponseMessage> SendAsync(Employee employee, string path, object? body = null)
    {
        var token = _factory.Services.GetRequiredService<IAccessTokenGenerator>().Generate(employee).Value;
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public FakeDecisionDispatcher Dispatcher { get; } = new();
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

    public sealed class FakeDecisionDispatcher : ICommandDispatcher
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
