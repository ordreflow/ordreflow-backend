using System.Net;
using System.Net.Http.Headers;
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

namespace UnitTests.TimeEntryReviews;

// The app reads JWT and seed settings from process-wide environment variables at startup.
[CollectionDefinition("Time entry reviews HTTP", DisableParallelization = true)]
public sealed class GetTimeEntryReviewsHttpCollection { }

[Collection("Time entry reviews HTTP")]
public class GetTimeEntryReviewsHttpTests : IClassFixture<GetTimeEntryReviewsHttpTests.ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;
    private readonly IAccessTokenGenerator _tokenGenerator;

    public GetTimeEntryReviewsHttpTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        _tokenGenerator = factory.Services.GetRequiredService<IAccessTokenGenerator>();
    }

    [Fact]
    public async Task Owner_GetsReviewHistoryWithReturnedReason()
    {
        using var response = await GetAsAsync(_factory.ReviewedEntry.Id.Value, _factory.Employee);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = json.RootElement.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());

        var expected = _factory.ReviewedEntry.Reviews
            .OrderBy(review => review.ReviewedAt)
            .ThenBy(review => review.Id.Value)
            .ToArray();
        for (var index = 0; index < expected.Length; index++)
        {
            var item = items[index];
            Assert.Equal(expected[index].Id.Value, item.GetProperty("id").GetGuid());
            Assert.Equal(_factory.ReviewedEntry.Id.Value, item.GetProperty("timeEntryId").GetGuid());
            Assert.Equal(expected[index].Decision.ToString(), item.GetProperty("decision").GetString());
            Assert.Equal(expected[index].ReviewedAt, item.GetProperty("reviewedAt").GetDateTime());
        }

        var returned = items.EnumerateArray().Single(item => item.GetProperty("decision").GetString() == "Returned");
        Assert.Equal("Needs correction", returned.GetProperty("reason").GetString());
        var accepted = items.EnumerateArray().Single(item => item.GetProperty("decision").GetString() == "Accepted");
        Assert.Equal(JsonValueKind.Null, accepted.GetProperty("reason").ValueKind);
    }

    [Fact]
    public async Task UnrelatedEmployee_GetsForbidden()
    {
        using var response = await GetAsAsync(_factory.ReviewedEntry.Id.Value, _factory.Outsider);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertErrorCodeAsync(response, "TimeEntryAccessForbidden");
    }

    [Fact]
    public async Task MissingToken_GetsUnauthorized()
    {
        using var response = await _client.GetAsync(PathFor(_factory.ReviewedEntry.Id.Value));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MissingEntry_GetsNotFound()
    {
        using var response = await GetAsAsync(
            Guid.Parse("7ce77b37-4c62-4fe7-b12c-3af101783044"), _factory.Employee);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertErrorCodeAsync(response, "TimeEntryNotFound");
    }

    [Fact]
    public async Task EntryWithoutReviews_GetsEmptyItemsArray()
    {
        using var response = await GetAsAsync(_factory.EmptyEntry.Id.Value, _factory.Employee);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = json.RootElement.GetProperty("items");
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.Equal(0, items.GetArrayLength());
    }

    private async Task<HttpResponseMessage> GetAsAsync(Guid entryId, User actor)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, PathFor(entryId));
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", _tokenGenerator.Generate(actor).Value);
        return await _client.SendAsync(request);
    }

    private static string PathFor(Guid entryId) => $"/api/time_entries/{entryId}/reviews";

    private static async Task AssertErrorCodeAsync(HttpResponseMessage response, string expectedCode)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedCode, Assert.Single(json.RootElement.EnumerateArray()).GetProperty("code").GetString());
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        public User Employee { get; }
        public User Outsider { get; }
        public TimeEntry ReviewedEntry { get; }
        public TimeEntry EmptyEntry { get; }

        public ApiFactory()
        {
            Environment.SetEnvironmentVariable("JWT_SECRET", "test-secret-that-is-at-least-32-bytes-long");
            Environment.SetEnvironmentVariable("JWT_ISSUER", null);
            Environment.SetEnvironmentVariable("JWT_AUDIENCE", null);
            // Whitespace prevents .env from supplying a seed account and is ignored by the seeder.
            Environment.SetEnvironmentVariable("ADMIN_EMAIL", " ");
            Environment.SetEnvironmentVariable("ADMIN_PASSWORD", " ");

            var admin = CreateUser("admin@example.com", UserRole.Admin);
            var manager = CreateUser("manager@example.com", UserRole.Manager);
            Employee = CreateUser("employee@example.com", UserRole.Employee);
            Outsider = CreateUser("outsider@example.com", UserRole.Employee);
            Assert.True(Employee.AssignManager(manager.UserId, manager.Role, manager.Status,
                admin.Role, admin.Status).IsSuccess);

            ReviewedEntry = CreateEntry(Employee);
            Assert.True(ReviewedEntry.Return(manager.UserId, Employee.UserId, Employee.ManagerId,
                manager.Role, manager.Status, "  Needs correction  ").IsSuccess);
            Assert.True(ReviewedEntry.Resubmit(Employee.UserId).IsSuccess);
            Assert.True(ReviewedEntry.Accept(manager.UserId, Employee.UserId, Employee.ManagerId,
                manager.Role, manager.Status).IsSuccess);
            EmptyEntry = CreateEntry(Employee);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ITimeEntryRepository>();
                services.RemoveAll<IUserRepository>();
                services.AddSingleton<ITimeEntryRepository>(
                    new FakeTimeEntryRepository(ReviewedEntry, EmptyEntry));
                services.AddSingleton<IUserRepository>(new FakeUserRepository(Employee, Outsider));
            });
        }

        private static User CreateUser(string email, UserRole role) =>
            User.Create(PersonName.Create("Test User").Value, EmailAddress.Create(email).Value, role).Value;

        private static TimeEntry CreateEntry(User employee) => TimeEntry.Create(
            employee.UserId,
            TaskId.Create(Guid.Parse("5ffdf238-4646-4581-baae-076118e053a8")).Value,
            new DateTime(2026, 9, 9), 7.5m, "Completed work").Value;
    }

    private sealed class FakeTimeEntryRepository(params TimeEntry[] entries) : ITimeEntryRepository
    {
        public Task<TimeEntry?> GetWithReviewsAsync(TimeEntryId id) =>
            Task.FromResult(entries.SingleOrDefault(entry => entry.Id == id));

        public Task<TimeEntry?> GetAsync(TimeEntryId id) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(UserId id) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByManagerIdAsync(UserId id) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(DateTime from, DateTime to) => throw new NotSupportedException();
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
}
