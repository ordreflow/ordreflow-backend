using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Dtos;
using Application.Interfaces;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebAPI.Contracts.Reports;

namespace WebApiTests.IntegrationTest;

[CollectionDefinition("Time reports HTTP", DisableParallelization = true)]
public sealed class TimeReportsHttpCollection { }

[Collection("Time reports HTTP")]
public sealed class TopCategoriesHttpTests : IClassFixture<TopCategoriesHttpTests.ApiFactory>
{
    private const string Path = "/api/reports/top-categories?fromDate=2026-11-01&toDate=2026-11-30";
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public TopCategoriesHttpTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        factory.Reports.Reset();
    }

    [Fact]
    public async Task Anonymous_requests_are_unauthorized_without_querying_totals()
    {
        using var response = await _client.GetAsync(Path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, _factory.Reports.Reads);
    }

    [Theory]
    [InlineData(SystemPermission.Employee, true)]
    [InlineData(SystemPermission.Payroll, true)]
    [InlineData(SystemPermission.CatagoryReviewer, true)]
    [InlineData(SystemPermission.DepartmentManager, false)]
    [InlineData(SystemPermission.Administrator, false)]
    public async Task Unrelated_grants_titles_and_missing_baseline_permission_are_forbidden(SystemPermission permission, bool baseline)
    {
        var actor = _factory.AddActor(permission);
        Assert.True(actor.ChangeProfile(actor.Name, actor.Email, "Manager").IsSuccess);
        if (!baseline) Assert.True(actor.RevokePermission(SystemPermission.Employee).IsSuccess);
        using var response = await GetAsync(Path, Token(actor));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, _factory.Reports.Reads);
    }

    [Fact]
    public async Task Deactivation_is_checked_again_for_an_existing_token()
    {
        var actor = _factory.AddActor(SystemPermission.DepartmentManager);
        var token = Token(actor);
        actor.Deactivate();
        using var response = await GetAsync(Path, token);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(0, _factory.Reports.Reads);
    }

    [Theory]
    [InlineData(SystemPermission.DepartmentManager)]
    [InlineData(SystemPermission.Administrator)]
    public async Task Report_returns_chart_ready_category_and_task_totals_using_authenticated_scope(SystemPermission permission)
    {
        var actor = _factory.AddActor(permission);
        var categoryId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        _factory.Reports.Rows = [new(categoryId, "Kundeprojekter", departmentId, "Installation", 7.5m,
            [new(Guid.NewGuid(), "Opsætning", 5m), new(Guid.NewGuid(), "Klargøring", 2.5m)])];

        // Client-supplied identity, department, and old status/limit parameters cannot broaden scope.
        using var response = await GetAsync(Path + $"&actorId={Guid.NewGuid()}&departmentId={Guid.NewGuid()}&onlyAccepted=true&limit=100", Token(actor));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetTopCategoriesResponse>();
        var category = Assert.Single(body!.Items);
        Assert.Equal(categoryId, category.CategoryId);
        Assert.Equal("Kundeprojekter", category.CategoryName);
        Assert.Equal(departmentId, category.DepartmentId);
        Assert.Equal(7.5m, category.TotalHours);
        Assert.Equal(new[] { 5m, 2.5m }, category.Tasks.Select(task => task.TotalHours));
        Assert.Equal(permission == SystemPermission.Administrator ? null : actor.Id, _factory.Reports.ManagerId);
        Assert.Equal(new DateOnly(2026, 11, 1), _factory.Reports.From);
        Assert.Equal(new DateOnly(2026, 11, 30), _factory.Reports.To);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("employeeId", json);
        Assert.DoesNotContain("comment", json);
    }

    [Fact]
    public async Task Reversed_dates_are_a_structured_error_and_missing_dates_are_rejected()
    {
        var actor = _factory.AddActor(SystemPermission.DepartmentManager);
        using var response = await GetAsync("/api/reports/top-categories?fromDate=2026-11-30&toDate=2026-11-01", Token(actor));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.Content.ReadFromJsonAsync<Core.Tools.OperationResult.Error[]>();
        Assert.Equal("InvalidDateRange", Assert.Single(errors!).Code);
        using var missing = await GetAsync("/api/reports/top-categories", Token(actor));
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(0, _factory.Reports.Reads);
    }

    [Theory]
    [InlineData("2026-11-01")]
    [InlineData("9999-12-31")]
    public async Task Empty_reports_and_inclusive_end_dates_are_supported_without_end_date_overflow(string end)
    {
        var actor = _factory.AddActor(SystemPermission.DepartmentManager);
        using var response = await GetAsync($"/api/reports/top-categories?fromDate=2026-11-01&toDate={end}", Token(actor));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<GetTopCategoriesResponse>())!.Items);
        Assert.Equal(DateOnly.ParseExact(end, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), _factory.Reports.To);
    }

    private string Token(Employee actor) => _factory.Services.GetRequiredService<IAccessTokenGenerator>().Generate(actor).Value;
    private async Task<HttpResponseMessage> GetAsync(string path, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public Employees Employees { get; } = new();
        public Reports Reports { get; } = new();
        public ApiFactory()
        {
            Environment.SetEnvironmentVariable("JWT_SECRET", new string('x', 64));
            Environment.SetEnvironmentVariable("ADMIN_EMAIL", " ");
            Environment.SetEnvironmentVariable("ADMIN_PASSWORD", " ");
        }
        public Employee AddActor(SystemPermission permission)
        {
            var actor = Employee.Create("User", $"{Guid.NewGuid():N}@test.invalid", DateTime.UtcNow).Value;
            Assert.True(actor.GrantPermission(permission).IsSuccess);
            Employees.Items.Add(actor.Id, actor);
            return actor;
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITopCategoriesQuery>();
            services.AddSingleton<ITopCategoriesQuery>(Reports);
            services.RemoveAll<IEmployeeAccessRepository>();
            services.AddSingleton<IEmployeeAccessRepository>(Employees);
        });
    }
    public sealed class Employees : IEmployeeAccessRepository
    {
        public Dictionary<EmployeeId, Employee> Items { get; } = [];
        public Task<Employee?> GetWithAccessesAsync(EmployeeId id, CancellationToken cancellationToken = default) => Task.FromResult(Items.GetValueOrDefault(id));
    }
    public sealed class Reports : ITopCategoriesQuery
    {
        public IReadOnlyList<TopCategoryReportDto> Rows { get; set; } = [];
        public int Reads { get; private set; }
        public EmployeeId? ManagerId { get; private set; }
        public DateOnly From { get; private set; }
        public DateOnly To { get; private set; }
        public void Reset() { Rows = []; Reads = 0; ManagerId = null; From = default; To = default; }
        public Task<IReadOnlyList<TopCategoryReportDto>> GetAsync(EmployeeId? managerId, DateOnly fromDate,
            DateOnly toDate, CancellationToken cancellationToken = default)
        {
            Reads++; ManagerId = managerId; From = fromDate; To = toDate;
            return Task.FromResult(Rows);
        }
    }
}
