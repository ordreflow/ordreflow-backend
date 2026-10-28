using System.Text.Json;
using Application.Dtos;
using Application.Handlers;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using WebAPI.Contracts.TimeEntries;
using WebAPI.MappingConfigurations;

namespace UnitTests.TimeEntryReviews;

public class GetTimeEntryReviewsTests
{
    [Fact]
    public async Task Owner_SeesReturnedReasonAndOrderedReviewHistory()
    {
        var (_, manager, employee, entry) = CreateAssignedEntry();
        Assert.True(entry.Return(manager.UserId, employee.UserId, employee.ManagerId,
            manager.Role, manager.Status, "  Needs correction  ").IsSuccess);
        Assert.True(entry.Resubmit(employee.UserId).IsSuccess);
        Assert.True(entry.Accept(manager.UserId, employee.UserId, employee.ManagerId,
            manager.Role, manager.Status).IsSuccess);
        Assert.True(entry.Finalize(manager.UserId, employee.UserId, employee.ManagerId,
            manager.Role, manager.Status).IsSuccess);
        var handler = CreateHandler(new FakeTimeEntryRepository(entry), employee);

        var result = await handler.HandleAsync(new GetTimeEntryReviewsQuery(employee.UserId.Value, entry.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        // The clock is owned by the domain; derive the expected order from the reviews it produced.
        var chronological = entry.Reviews.OrderBy(review => review.ReviewedAt)
            .ThenBy(review => review.Id.Value).ToArray();
        Assert.Equal(chronological.Select(review => review.Id.Value), result.Value.Select(review => review.Id));

        var byId = result.Value.ToDictionary(review => review.Id);
        foreach (var review in entry.Reviews)
        {
            var dto = byId[review.Id.Value];
            Assert.Equal(entry.Id.Value, dto.TimeEntryId);
            Assert.Equal(review.Decision.ToString(), dto.Decision);
            Assert.Equal(review.Reason, dto.Reason);
            Assert.Equal(review.ReviewedAt, dto.ReviewedAt);
        }

        var returned = entry.Reviews.Single(review => review.Decision == ReviewDecision.Returned);
        Assert.Equal("Returned", byId[returned.Id.Value].Decision);
        Assert.Equal("Needs correction", byId[returned.Id.Value].Reason);
        Assert.Null(byId[entry.Reviews.Single(review => review.Decision == ReviewDecision.Accepted).Id.Value].Reason);
        Assert.Null(byId[entry.Reviews.Single(review => review.Decision == ReviewDecision.Finalized).Id.Value].Reason);
    }

    [Theory]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Admin)]
    public async Task AssignedManagerAndAdmin_CanSeeReviewHistory(UserRole role)
    {
        var (admin, manager, employee, entry) = CreateAssignedEntry();
        Assert.True(entry.Return(manager.UserId, employee.UserId, employee.ManagerId,
            manager.Role, manager.Status, "Needs correction").IsSuccess);
        var actor = role == UserRole.Manager ? manager : admin;
        var handler = CreateHandler(new FakeTimeEntryRepository(entry), actor, employee);

        var result = await handler.HandleAsync(new GetTimeEntryReviewsQuery(actor.UserId.Value, entry.Id.Value));

        Assert.True(result.IsSuccess);
        var review = Assert.Single(result.Value);
        Assert.Equal(entry.Reviews.Single().Id.Value, review.Id);
        Assert.Equal(entry.Id.Value, review.TimeEntryId);
        Assert.Equal("Returned", review.Decision);
        Assert.Equal("Needs correction", review.Reason);
    }

    [Theory]
    [InlineData(UserRole.Employee)]
    [InlineData(UserRole.Manager)]
    public async Task UnrelatedEmployeeOrManager_CannotSeeReviews(UserRole role)
    {
        var (_, _, employee, entry) = CreateAssignedEntry();
        var outsider = CreateUser("outsider@example.com", role);
        var handler = CreateHandler(new FakeTimeEntryRepository(entry), outsider, employee);

        var result = await handler.HandleAsync(new GetTimeEntryReviewsQuery(outsider.UserId.Value, entry.Id.Value));

        AssertError("TimeEntryAccessForbidden", result);
    }

    [Fact]
    public async Task ManagerOfUnassignedEmployee_CannotSeeReviews()
    {
        var employee = CreateUser("employee@example.com");
        var manager = CreateUser("manager@example.com", UserRole.Manager);
        var entry = CreateEntry(employee);
        var handler = CreateHandler(new FakeTimeEntryRepository(entry), manager, employee);

        var result = await handler.HandleAsync(new GetTimeEntryReviewsQuery(manager.UserId.Value, entry.Id.Value));

        AssertError("TimeEntryAccessForbidden", result);
    }

    [Theory]
    [InlineData(UserRole.Employee)]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Admin)]
    public async Task InactiveActor_CannotSeeReviewsEvenWithScope(UserRole role)
    {
        var (admin, manager, employee, entry) = CreateAssignedEntry();
        var actor = role switch
        {
            UserRole.Manager => manager,
            UserRole.Admin => admin,
            _ => employee
        };
        Assert.True(actor.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);
        var handler = role == UserRole.Employee
            ? CreateHandler(new FakeTimeEntryRepository(entry), employee)
            : CreateHandler(new FakeTimeEntryRepository(entry), actor, employee);

        var result = await handler.HandleAsync(new GetTimeEntryReviewsQuery(actor.UserId.Value, entry.Id.Value));

        AssertError("TimeEntryAccessForbidden", result);
    }

    [Fact]
    public async Task MissingTimeEntry_ReturnsNotFound()
    {
        var employee = CreateUser("employee@example.com");
        var handler = CreateHandler(new FakeTimeEntryRepository(), employee);

        var result = await handler.HandleAsync(new GetTimeEntryReviewsQuery(
            employee.UserId.Value, Guid.Parse("9b03137a-4ed6-42f5-a918-048ad0804bd2")));

        AssertError("TimeEntryNotFound", result);
    }

    [Fact]
    public async Task EntryWithoutReviews_ReturnsEmptyList()
    {
        var employee = CreateUser("employee@example.com");
        var entry = CreateEntry(employee);
        var handler = CreateHandler(new FakeTimeEntryRepository(entry), employee);

        var result = await handler.HandleAsync(new GetTimeEntryReviewsQuery(employee.UserId.Value, entry.Id.Value));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Theory]
    [InlineData(true, "UserIdEmpty")]
    [InlineData(false, "TimeEntryIdEmpty")]
    public async Task EmptyIdentifiers_ReturnValidationErrors(bool invalidActor, string expectedCode)
    {
        var employee = CreateUser("employee@example.com");
        var entry = CreateEntry(employee);
        var handler = CreateHandler(new FakeTimeEntryRepository(entry), employee);

        var result = await handler.HandleAsync(new GetTimeEntryReviewsQuery(
            invalidActor ? Guid.Empty : employee.UserId.Value,
            invalidActor ? entry.Id.Value : Guid.Empty));

        AssertError(expectedCode, result);
    }

    [Fact]
    public async Task UnknownActor_ReturnsUserNotFound()
    {
        var employee = CreateUser("employee@example.com");
        var entry = CreateEntry(employee);
        var handler = CreateHandler(new FakeTimeEntryRepository(entry), employee);

        var result = await handler.HandleAsync(new GetTimeEntryReviewsQuery(
            Guid.Parse("cb6a5b05-d7f8-49aa-9210-35eddf0bed38"), entry.Id.Value));

        AssertError("UserNotFound", result);
    }

    [Fact]
    public void ResponseMapper_PreservesReviewDetailsInWebJson()
    {
        var dto = new TimeEntryReviewDto(
            Guid.Parse("479be205-6659-4a49-87c9-61b693c4f7e0"),
            Guid.Parse("34841e23-0915-452d-a5a8-44f7cb2a01d4"),
            "Returned", "Needs correction", new DateTime(2026, 9, 9, 10, 30, 0, DateTimeKind.Utc));
        var accepted = dto with { Id = Guid.Parse("d09ad2eb-59f9-4f2f-a435-3ff06b05e363"),
            Decision = "Accepted", Reason = null };
        var mapper = new TimeEntryReviewToResponseMapper();
        var response = new GetTimeEntryReviewsResponse(new[] { mapper.Map(dto), mapper.Map(accepted) });

        var json = JsonSerializer.SerializeToElement(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var items = json.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal(dto.Id, items[0].GetProperty("id").GetGuid());
        Assert.Equal(dto.TimeEntryId, items[0].GetProperty("timeEntryId").GetGuid());
        Assert.Equal("Returned", items[0].GetProperty("decision").GetString());
        Assert.Equal("Needs correction", items[0].GetProperty("reason").GetString());
        Assert.Equal(dto.ReviewedAt, items[0].GetProperty("reviewedAt").GetDateTime());
        Assert.Equal(accepted.Id, items[1].GetProperty("id").GetGuid());
        Assert.Equal("Accepted", items[1].GetProperty("decision").GetString());
        Assert.Equal(JsonValueKind.Null, items[1].GetProperty("reason").ValueKind);
    }

    private static (User Admin, User Manager, User Employee, TimeEntry Entry) CreateAssignedEntry()
    {
        var admin = CreateUser("admin@example.com", UserRole.Admin);
        var manager = CreateUser("manager@example.com", UserRole.Manager);
        var employee = CreateUser("employee@example.com");
        Assert.True(employee.AssignManager(manager.UserId, manager.Role, manager.Status,
            admin.Role, admin.Status).IsSuccess);
        return (admin, manager, employee, CreateEntry(employee));
    }

    private static User CreateUser(string email, UserRole role = UserRole.Employee) =>
        User.Create(PersonName.Create("Test User").Value, EmailAddress.Create(email).Value, role).Value;

    private static TimeEntry CreateEntry(User employee) => TimeEntry.Create(
        employee.UserId,
        TaskId.Create(Guid.Parse("6bcb1c4f-c171-4f0a-b17a-e49d3a145dc0")).Value,
        new DateTime(2026, 9, 9), 7.5m, "Completed work").Value;

    private static GetTimeEntryReviewsHandler CreateHandler(ITimeEntryRepository entries, params User[] users) =>
        new(entries, new FakeUserRepository(users));

    private static void AssertError(string code, Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, Assert.Single(result.Errors).Code);
    }

    private sealed class FakeTimeEntryRepository(params TimeEntry[] entries) : ITimeEntryRepository
    {
        public Task<TimeEntry?> GetWithReviewsAsync(TimeEntryId id) =>
            Task.FromResult(entries.SingleOrDefault(entry => entry.Id == id));

        public Task<TimeEntry?> GetAsync(TimeEntryId id) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(UserId employeeId) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
        public Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(DateTime from, DateTime to) => throw new NotSupportedException();
        public Task<bool> ExistsForTaskAsync(TaskId taskId) => throw new NotSupportedException();
        public Task AddAsync(TimeEntry entity) => throw new NotSupportedException();
        public Task RemoveAsync(TimeEntryId id) => throw new NotSupportedException();
    }

    private sealed class FakeUserRepository(params User[] users) : IUserRepository
    {
        public Task<User?> GetAsync(UserId id) =>
            Task.FromResult(users.SingleOrDefault(user => user.UserId == id));

        public Task<User?> GetByEmailAsync(EmailAddress email) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> GetAllAsync() => throw new NotSupportedException();
        public Task AddAsync(User entity) => throw new NotSupportedException();
        public Task RemoveAsync(UserId id) => throw new NotSupportedException();
    }
}
