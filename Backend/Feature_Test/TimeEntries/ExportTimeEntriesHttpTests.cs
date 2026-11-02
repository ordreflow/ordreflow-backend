using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace UnitTests.TimeEntries;

// The app reads JWT and seed settings from process-wide environment variables at startup.
[CollectionDefinition("Time entry export HTTP", DisableParallelization = true)]
public sealed class ExportTimeEntriesHttpCollection { }

[Collection("Time entry export HTTP")]
public class ExportTimeEntriesHttpTests : IClassFixture<ExportTimeEntriesHttpTests.ApiFactory>
{
    private const string Path = "/api/time_entries/export?FromDate=2026-10-01&ToDate=2026-10-31";

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;
    private readonly IAccessTokenGenerator _tokenGenerator;

    public ExportTimeEntriesHttpTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        _tokenGenerator = factory.Services.GetRequiredService<IAccessTokenGenerator>();
    }

    [Fact]
    public async Task Manager_GetsCsvFileWithAcceptedEntriesOnly()
    {
        using var response = await GetAsAsync(Path, _factory.Manager);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("time-entries-20261001-20261031.csv",
            response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
        var lines = Encoding.UTF8.GetString(bytes[3..]).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("2026-10-05,Anna Andersen,Netto Vestergade,Opsæt reoler,7.5,", lines[1]);
        Assert.EndsWith(_factory.AcceptedEntry.Id.Value.ToString(), lines[1]);
    }

    [Fact]
    public async Task Employee_GetsForbidden()
    {
        using var response = await GetAsAsync(Path, _factory.Employee);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MissingToken_GetsUnauthorized()
    {
        using var response = await _client.GetAsync(Path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EndBeforeStart_GetsBadRequest()
    {
        using var response = await GetAsAsync(
            "/api/time_entries/export?FromDate=2026-10-31&ToDate=2026-10-01", _factory.Manager);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("InvalidDateRange",
            Assert.Single(json.RootElement.EnumerateArray()).GetProperty("code").GetString());
    }

    private async Task<HttpResponseMessage> GetAsAsync(string path, User actor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", _tokenGenerator.Generate(actor).Value);
        return await _client.SendAsync(request);
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public User Manager { get; }
        public User Employee { get; }
        public TimeEntry AcceptedEntry { get; }

        private readonly Order _order;
        private readonly Domain.Entities.Task _task;
        private readonly TimeEntry _draftEntry;

        public ApiFactory()
        {
            Environment.SetEnvironmentVariable("JWT_SECRET", "test-secret-that-is-at-least-32-bytes-long");
            Environment.SetEnvironmentVariable("JWT_ISSUER", null);
            Environment.SetEnvironmentVariable("JWT_AUDIENCE", null);
            // Whitespace prevents .env from supplying a seed account and is ignored by the seeder.
            Environment.SetEnvironmentVariable("ADMIN_EMAIL", " ");
            Environment.SetEnvironmentVariable("ADMIN_PASSWORD", " ");

            var admin = CreateUser("Admin", "admin@example.com", UserRole.Admin);
            Manager = CreateUser("Mette Madsen", "mette@example.com", UserRole.Manager);
            Employee = CreateUser("Anna Andersen", "anna@example.com", UserRole.Employee);
            Assert.True(Employee.AssignManager(Manager.UserId, Manager.Role, Manager.Status,
                admin.Role, admin.Status).IsSuccess);

            _order = Order.Create(Manager.UserId, Manager.Role, Manager.Status,
                OrderName.Create("Netto Vestergade").Value).Value;
            _task = _order.AddWorkItem(Manager.UserId, Manager.Role, Manager.Status, "Opsæt reoler", "Reoler").Value;

            AcceptedEntry = TimeEntry.Create(Employee.UserId, _task.TaskId, new DateTime(2026, 10, 5), 7.5m, null).Value;
            Assert.True(AcceptedEntry.Accept(Manager.UserId, Employee.UserId, Employee.ManagerId,
                Manager.Role, Manager.Status).IsSuccess);
            _draftEntry = TimeEntry.Create(Employee.UserId, _task.TaskId, new DateTime(2026, 10, 6), 3m, null).Value;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ITimeEntryRepository>();
                services.RemoveAll<IUserRepository>();
                services.RemoveAll<ITaskRepository>();
                services.RemoveAll<IOrderRepository>();
                services.AddSingleton<ITimeEntryRepository>(new FakeTimeEntryRepository(AcceptedEntry, _draftEntry));
                services.AddSingleton<IUserRepository>(new FakeUserRepository(Manager, Employee));
                services.AddSingleton<ITaskRepository>(new FakeTaskRepository(_task));
                services.AddSingleton<IOrderRepository>(new FakeOrderRepository(_order));
            });
        }

        private static User CreateUser(string name, string email, UserRole role) =>
            User.Create(PersonName.Create(name).Value, EmailAddress.Create(email).Value, role).Value;
    }

    private sealed class FakeTimeEntryRepository(params TimeEntry[] entries) : ITimeEntryRepository
    {
        public Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(DateTime from, DateTime to) =>
            Task.FromResult<IReadOnlyList<TimeEntry>>(
                entries.Where(entry => entry.Date >= from && entry.Date < to).ToArray());

        public Task<TimeEntry?> GetWithReviewsAsync(TimeEntryId id) => throw new NotSupportedException();
        public Task<TimeEntry?> GetAsync(TimeEntryId id) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(UserId id) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByManagerIdAsync(UserId id) => throw new NotSupportedException();
        public Task<bool> ExistsForTaskAsync(TaskId id) => throw new NotSupportedException();
        public Task AddAsync(TimeEntry entity) => throw new NotSupportedException();
        public Task RemoveAsync(TimeEntryId id) => throw new NotSupportedException();
    }

    private sealed class FakeUserRepository(params User[] users) : IUserRepository
    {
        public Task<User?> GetAsync(UserId id) =>
            Task.FromResult(users.SingleOrDefault(user => user.UserId == id));

        public Task<User?> GetByEmailAsync(EmailAddress email) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> GetByManagerIdAsync(UserId id) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> GetAllAsync() => throw new NotSupportedException();
        public Task AddAsync(User entity) => throw new NotSupportedException();
        public Task RemoveAsync(UserId id) => throw new NotSupportedException();
    }

    private sealed class FakeTaskRepository(params Domain.Entities.Task[] tasks) : ITaskRepository
    {
        public Task<Domain.Entities.Task?> GetAsync(TaskId id) =>
            Task.FromResult(tasks.SingleOrDefault(task => task.TaskId == id));

        public Task<IReadOnlyList<Domain.Entities.Task>> GetByOrderIdAsync(OrderId orderId) =>
            throw new NotSupportedException();
    }

    private sealed class FakeOrderRepository(params Order[] orders) : IOrderRepository
    {
        public Task<Order?> GetAsync(OrderId id) =>
            Task.FromResult(orders.SingleOrDefault(order => order.Id == id));

        public Task<IReadOnlyList<Order>> GetByManagerIdAsync(UserId id) => throw new NotSupportedException();
        public Task<IReadOnlyList<Order>> GetAllAsync() => throw new NotSupportedException();
        public Task AddAsync(Order entity) => throw new NotSupportedException();
        public Task RemoveAsync(OrderId id) => throw new NotSupportedException();
    }
}
