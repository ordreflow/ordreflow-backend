using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Application.Exports;
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
using TimeEntryAudit = Domain.Entities.TimeEntryAudit;
using WebAPI.Contracts.Exports;

namespace UnitTests.TimeEntries;

[CollectionDefinition("Payroll exports HTTP", DisableParallelization = true)]
public sealed class PayrollExportsHttpCollection { }

[Collection("Payroll exports HTTP")]
public sealed class PayrollExportsHttpTests : IClassFixture<PayrollExportsHttpTests.ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public PayrollExportsHttpTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        factory.Repository.Reset();
    }

    [Fact]
    public async Task Creating_and_downloading_a_batch_does_not_close_entries_but_explicit_action_does()
    {
        var id = Guid.NewGuid();
        using var created = await SendAsync(HttpMethod.Post, "/api/exports", _factory.Payroll, Request(id));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal($"/api/exports/{id}", created.Headers.Location?.ToString());
        var details = await created.Content.ReadFromJsonAsync<ExportDetailsDto>();
        Assert.Equal(id, details!.Export.Id);
        Assert.Null(details.Export.TimeEntriesClosedAtUtc);
        Assert.Equal(TimeEntryStatus.Approved, _factory.Repository.Entry.CurrentStatus);

        using var download = await SendAsync(HttpMethod.Get, details.DownloadUrl, _factory.Payroll);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("text/csv", download.Content.Headers.ContentType?.MediaType);
        var bytes = await download.Content.ReadAsByteArrayAsync();
        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
        Assert.Contains(",Approved,", Encoding.UTF8.GetString(bytes));
        Assert.Equal(TimeEntryStatus.Approved, _factory.Repository.Entry.CurrentStatus);

        using var close = await SendAsync(HttpMethod.Post, $"/api/exports/{id}/close-time-entries", _factory.Payroll);
        Assert.Equal(HttpStatusCode.NoContent, close.StatusCode);
        Assert.Equal(TimeEntryStatus.Closed, _factory.Repository.Entry.CurrentStatus);
        using var again = await SendAsync(HttpMethod.Get, details.DownloadUrl, _factory.Payroll);
        Assert.Equal(bytes, await again.Content.ReadAsByteArrayAsync());
        using var repeat = await SendAsync(HttpMethod.Post, $"/api/exports/{id}/close-time-entries", _factory.Payroll);
        Assert.Equal(HttpStatusCode.NoContent, repeat.StatusCode);
        Assert.Equal(3, _factory.Repository.Entry.Audits.Count);
    }

    [Fact]
    public async Task Missing_authentication_is_unauthorized_and_employee_access_alone_is_forbidden()
    {
        using var anonymous = await _client.PostAsJsonAsync("/api/exports", Request(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        using var employee = await SendAsync(HttpMethod.Post, "/api/exports", _factory.Employee, Request(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Forbidden, employee.StatusCode);
        Assert.Empty(_factory.Repository.Batches);
    }

    [Fact]
    public async Task Empty_selection_is_structured_and_does_not_save_a_batch()
    {
        using var response = await SendAsync(HttpMethod.Post, "/api/exports", _factory.Payroll,
            Request(Guid.NewGuid()) with { FromDate = new DateOnly(2026, 12, 1), ToDate = new DateOnly(2026, 12, 2) });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("NoApprovedEntries", Assert.Single((await response.Content.ReadFromJsonAsync<Error[]>())!).Code);
        Assert.Empty(_factory.Repository.Batches);
    }

    private static CreateExportRequest Request(Guid id) => new(id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, Employee actor, object? body = null)
    {
        var token = _factory.Services.GetRequiredService<IAccessTokenGenerator>().Generate(actor).Value;
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public Employee Payroll { get; } = Domain.Aggregate.Employee.Create("Payroll", "payroll@test.invalid", DateTime.UtcNow).Value;
        public Employee Employee { get; } = Domain.Aggregate.Employee.Create("Worker", "worker@test.invalid", DateTime.UtcNow).Value;
        public Exports Repository { get; }

        public ApiFactory()
        {
            Environment.SetEnvironmentVariable("JWT_SECRET", new string('x', 64));
            Environment.SetEnvironmentVariable("ADMIN_EMAIL", " ");
            Environment.SetEnvironmentVariable("ADMIN_PASSWORD", " ");
            Assert.True(Payroll.GrantPermission(SystemPermission.Payroll).IsSuccess);
            Repository = new Exports(Employee.Id, Payroll.Id);
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IExportRepository>();
            services.AddSingleton<IExportRepository>(Repository);
            services.RemoveAll<IEmployeeAccessRepository>();
            services.AddSingleton<IEmployeeAccessRepository>(new Employees(Payroll, Employee));
        });
    }

    public sealed class Employees(params Employee[] employees) : IEmployeeAccessRepository
    {
        public Task<Employee?> GetWithAccessesAsync(EmployeeId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(employees.SingleOrDefault(item => item.Id == id));
    }

    public sealed class Exports(EmployeeId owner, EmployeeId reviewer) : IExportRepository
    {
        public TimeEntry Entry { get; private set; } = null!;
        public Dictionary<ExportId, Export> Batches { get; } = [];
        public void Reset()
        {
            Batches.Clear();
            var id = TimeEntryId.Create(Guid.NewGuid()).Value;
            var created = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
            Entry = TimeEntry.Create(owner, TimeEntryAudit.Create(id, owner, TaskId.Create(Guid.NewGuid()).Value,
                WorkDate.Create(new DateOnly(2026, 11, 5)).Value, Duration.Create(7.5m).Value,
                TimeEntryDescription.Create("Work").Value, TimeEntryStatus.ReadyForApproval, created).Value).Value;
            Assert.True(Entry.Approve(reviewer, created.AddMinutes(1), true).IsSuccess);
        }
        public Task<bool> EmployeeExistsAsync(EmployeeId id, CancellationToken ct) => Task.FromResult(id == owner);
        public Task<IReadOnlyList<ExportCandidate>> SelectApprovedAsync(DateOnly from, DateOnly to, EmployeeId? employee, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ExportCandidate>>(Entry.CurrentStatus == TimeEntryStatus.Approved &&
                Entry.CurrentWorkDate!.Value >= from && Entry.CurrentWorkDate.Value <= to && (employee is null || employee == owner)
                    ? [new ExportCandidate(Entry, "Worker", "Task", "Category", "Department")] : []);
        public Task<Export?> GetAsync(ExportId id, CancellationToken ct) => Task.FromResult(Batches.GetValueOrDefault(id));
        public Task<Result<Export>> SaveNewAsync(Export batch, CancellationToken ct)
        {
            Batches.Add(batch.Id, batch);
            return Task.FromResult(Result<Export>.Success(batch));
        }
        public Task<Result> CloseTimeEntriesAsync(ExportId id, EmployeeId actor, DateTime utc, CancellationToken ct) =>
            Task.FromResult(Batches.TryGetValue(id, out var batch) ? batch.CloseTimeEntries([Entry], actor, utc)
                : Result.Failure(new Error("ExportNotFound", "Not found.")));
        public Task<IReadOnlyList<ExportSummaryDto>> ListAsync(int page, int size, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ExportSummaryDto>>(Batches.Values.Select(PayrollExportService.Summary).ToArray());
        public Task<IReadOnlyList<ExportEmployeeDto>> EmployeesAsync(string? search, int page, int size, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ExportEmployeeDto>>([new ExportEmployeeDto(owner.Value, "Worker", "Active")]);
    }
}
