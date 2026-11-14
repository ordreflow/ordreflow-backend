using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Application;
using Application.Dtos;
using Application.Interfaces;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using WebAPI.Contracts.TimeEntries;

namespace WebApiTests.IntegrationTest;

[CollectionDefinition("Reviewer time entry HTTP", DisableParallelization = true)]
public sealed class ReviewerTimeEntryHttpCollection { }

[Collection("Reviewer time entry HTTP")]
public sealed class ReviewerTimeEntriesHttpTests : IClassFixture<ReviewerTimeEntriesHttpTests.ApiFactory>
{
    private const string Path = "/api/time_entries/reviewer?fromDate=2026-11-14&toDate=2026-11-16";

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ReviewerTimeEntriesHttpTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        _factory.Dispatcher.Reset();
    }

    [Fact]
    public async Task Anonymous_request_is_unauthorized_before_dispatch()
    {
        using var response = await _client.GetAsync(Path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(_factory.Dispatcher.LastQuery);
    }

    [Fact]
    public async Task Employee_without_reviewer_permission_is_forbidden_before_dispatch()
    {
        var employee = AddEmployee();

        using var response = await GetAsEmployeeAsync(employee.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(_factory.Dispatcher.LastQuery);
    }

    [Fact]
    public async Task Deactivated_reviewer_is_forbidden_even_with_an_existing_token()
    {
        var employee = AddEmployee(reviewer: true);
        employee.Status = EmployeeStatus.Deactivated;

        using var response = await GetAsEmployeeAsync(employee.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(_factory.Dispatcher.LastQuery);
    }

    [Fact]
    public async Task Reviewer_without_baseline_employee_access_is_forbidden()
    {
        var employee = new Employee
        {
            Id = Guid.NewGuid(), Name = "Reviewer", Email = "reviewer@test.invalid",
            Status = EmployeeStatus.Active, CreatedAt = DateTime.UtcNow
        };
        Assert.True(employee.GrantPermission(SystemPermission.CatagoryReviewer).IsSuccess);
        _factory.Access.Add(employee);

        using var response = await GetAsEmployeeAsync(employee.Id);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(_factory.Dispatcher.LastQuery);
    }

    [Fact]
    public async Task Unrefactored_routes_fail_closed_for_authenticated_employees()
    {
        var employee = AddEmployee(reviewer: true);

        using var response = await GetAsEmployeeAsync(employee.Id, "/api/users/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(_factory.Dispatcher.LastQuery);
    }

    [Fact]
    public async Task Reviewer_scope_failure_from_application_is_forbidden()
    {
        var reviewerId = AddEmployee(reviewer: true).Id;
        _factory.Dispatcher.Result = Result<IReadOnlyList<TimeEntryDto>>.Failure(
            new Error("ReviewForbidden", "An active category reviewer permission is required."));

        using var response = await GetAsEmployeeAsync(reviewerId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("ReviewForbidden", body.RootElement[0].GetProperty("code").GetString());
        Assert.Equal(reviewerId, _factory.Dispatcher.LastQuery?.ReviewerId);
    }

    [Fact]
    public async Task Reviewer_without_identity_role_can_dispatch_query_with_existing_response_fields_and_inclusive_dates()
    {
        var reviewerId = AddEmployee(reviewer: true).Id;
        var entryId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        _factory.Dispatcher.Result = Result<IReadOnlyList<TimeEntryDto>>.Success(
            [new TimeEntryDto(entryId, taskId, new DateTime(2026, 11, 14), 2.25m,
                "Completed work", employeeId, "ReadyForApproval")]);

        using var response = await GetAsEmployeeAsync(reviewerId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = Assert.Single((await response.Content.ReadFromJsonAsync<ViewTimeEntriesResponse>())!.Items);
        Assert.Equal(entryId, item.Id);
        Assert.Equal(taskId, item.TaskId);
        Assert.Equal(employeeId, item.EmployeeId);
        Assert.Equal(new DateTime(2026, 11, 14), item.Date);
        Assert.Equal(2.25m, item.Hours);
        Assert.Equal("Completed work", item.Comment);
        Assert.Equal("ReadyForApproval", item.Status);
        Assert.Equal(reviewerId, _factory.Dispatcher.LastQuery?.ReviewerId);
        Assert.Equal(new DateTime(2026, 11, 14), _factory.Dispatcher.LastQuery?.FromDate);
        Assert.Equal(new DateTime(2026, 11, 17), _factory.Dispatcher.LastQuery?.ToDateExclusive);
        Assert.Null(_factory.Dispatcher.LastQuery?.Status);
    }

    private async Task<HttpResponseMessage> GetAsEmployeeAsync(Guid reviewerId, string path = Path)
    {
        var token = _factory.Services.GetRequiredService<IAccessTokenGenerator>()
            .Generate(_factory.Access.Get(reviewerId)).Value;
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        Assert.Equal(reviewerId.ToString(), jwt.Subject);
        Assert.DoesNotContain(jwt.Claims, claim => claim.Type == "role");
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private Employee AddEmployee(bool reviewer = false)
    {
        var employee = Employee.Create("Reviewer", $"{Guid.NewGuid():N}@test.invalid", DateTime.UtcNow).Value;
        if (reviewer)
            Assert.True(employee.GrantPermission(SystemPermission.CatagoryReviewer).IsSuccess);
        _factory.Access.Add(employee);
        return employee;
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public FakeQueryDispatcher Dispatcher { get; } = new();
        public FakeEmployeeAccessRepository Access { get; } = new();

        public ApiFactory()
        {
            Environment.SetEnvironmentVariable("JWT_SECRET", new string('x', 64));
            Environment.SetEnvironmentVariable("JWT_ISSUER", null);
            Environment.SetEnvironmentVariable("JWT_AUDIENCE", null);
            Environment.SetEnvironmentVariable("ADMIN_EMAIL", " ");
            Environment.SetEnvironmentVariable("ADMIN_PASSWORD", " ");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IQueryDispatcher>();
                services.AddSingleton<IQueryDispatcher>(Dispatcher);
                services.RemoveAll<IEmployeeAccessRepository>();
                services.AddSingleton<IEmployeeAccessRepository>(Access);
            });
        }
    }

    public sealed class FakeEmployeeAccessRepository : IEmployeeAccessRepository
    {
        private readonly Dictionary<Guid, Employee> _employees = new();

        public void Add(Employee employee) => _employees.Add(employee.Id, employee);

        public Employee Get(Guid employeeId) => _employees[employeeId];

        public Task<Employee?> GetWithAccessesAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_employees.GetValueOrDefault(employeeId));
    }

    public sealed class FakeQueryDispatcher : IQueryDispatcher
    {
        public GetReviewerTimeEntriesQuery? LastQuery { get; private set; }
        public Result<IReadOnlyList<TimeEntryDto>> Result { get; set; } =
            Result<IReadOnlyList<TimeEntryDto>>.Success(Array.Empty<TimeEntryDto>());

        public void Reset()
        {
            LastQuery = null;
            Result = Result<IReadOnlyList<TimeEntryDto>>.Success(Array.Empty<TimeEntryDto>());
        }

        public Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query)
        {
            LastQuery = Assert.IsType<GetReviewerTimeEntriesQuery>(query);
            return Task.FromResult((TResult)(object)Result);
        }
    }
}
