using System.Text.Json;
using Application.Dtos;
using Application.Handlers;
using Application.Queries;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using WebAPI.MappingConfigurations;

namespace UnitTests.TimeEntries;

public class TimeEntryResponseTests
{
    [Fact]
    public async Task GetManagerTimeEntries_IncludesEachEmployeesIdAndStatus()
    {
        var admin = CreateUser("admin@example.com", UserRole.Admin);
        var manager = CreateUser("manager@example.com", UserRole.Manager);
        var otherManager = CreateUser("other.manager@example.com", UserRole.Manager);
        var firstEmployee = CreateUser("first@example.com");
        var secondEmployee = CreateUser("second@example.com");
        var otherEmployee = CreateUser("other@example.com");
        AssignManager(firstEmployee, manager, admin);
        AssignManager(secondEmployee, manager, admin);
        AssignManager(otherEmployee, otherManager, admin);
        var draft = CreateEntry(firstEmployee, new DateTime(2026, 9, 7));
        var accepted = CreateEntry(secondEmployee, new DateTime(2026, 9, 8));
        var outOfScope = CreateEntry(otherEmployee, new DateTime(2026, 9, 9));
        Accept(accepted, manager);
        var handler = new GetManagerTimeEntriesHandler(
            new FakeTimeEntryRepository(new[] { firstEmployee, secondEmployee, otherEmployee },
                draft, accepted, outOfScope),
            new FakeUserRepository(manager));

        var result = await handler.HandleAsync(new GetManagerTimeEntriesQuery(manager.UserId.Value, null, null, null));

        Assert.True(result.IsSuccess);
        var entries = result.Value.ToDictionary(entry => entry.Id);
        Assert.Equal(2, entries.Count);
        AssertOwnerAndStatus(draft, entries[draft.Id.Value], "Draft");
        AssertOwnerAndStatus(accepted, entries[accepted.Id.Value], "Accepted");
        Assert.DoesNotContain(outOfScope.Id.Value, entries.Keys);
    }

    [Fact]
    public async Task GetMyTimeEntriesForWeek_IncludesEmployeeIdAndReturnedStatus()
    {
        var employee = CreateUser("employee@example.com");
        var manager = CreateUser("manager@example.com", UserRole.Manager);
        var entry = CreateEntry(employee, new DateTime(2026, 9, 9));
        Assert.True(entry.Return(manager.UserId, employee.UserId, manager.UserId,
            manager.Role, manager.Status, "Needs correction").IsSuccess);
        var handler = new GetMyTimeEntriesForWeekHandler(new FakeTimeEntryRepository(new[] { employee }, entry));

        var result = await handler.HandleAsync(new GetMyTimeEntriesForWeekQuery(
            employee.UserId.Value, new DateTime(2026, 9, 7), new DateTime(2026, 9, 14)));

        Assert.True(result.IsSuccess);
        AssertOwnerAndStatus(entry, Assert.Single(result.Value), "Returned");
    }

    [Fact]
    public async Task GetTimeEntry_IncludesEmployeeIdAndFinalizedStatus()
    {
        var employee = CreateUser("employee@example.com");
        var manager = CreateUser("manager@example.com", UserRole.Manager);
        var entry = CreateEntry(employee, new DateTime(2026, 9, 9));
        Accept(entry, manager);
        Assert.True(entry.Finalize(manager.UserId, employee.UserId, manager.UserId,
            manager.Role, manager.Status).IsSuccess);
        var handler = new GetTimeEntryHandler(
            new FakeTimeEntryRepository(new[] { employee }, entry), new FakeUserRepository(employee));

        var result = await handler.HandleAsync(new GetTimeEntryQuery(employee.UserId.Value, entry.Id.Value));

        Assert.True(result.IsSuccess);
        AssertOwnerAndStatus(entry, result.Value, "Finalized");
    }

    [Fact]
    public async Task GetTimeEntry_InactiveOwnerCannotViewEntry()
    {
        var employee = CreateUser("employee@example.com");
        var entry = CreateEntry(employee, new DateTime(2026, 9, 9));
        Assert.True(employee.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);
        var handler = new GetTimeEntryHandler(
            new FakeTimeEntryRepository(new[] { employee }, entry), new FakeUserRepository(employee));

        var result = await handler.HandleAsync(new GetTimeEntryQuery(employee.UserId.Value, entry.Id.Value));

        Assert.True(result.IsFailure);
        Assert.Equal("TimeEntryAccessForbidden", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void TimeEntryToResponseMapper_ExposesEmployeeIdAndStatusInWebJson()
    {
        var employeeId = Guid.Parse("e47b48ba-01cf-4c2b-902a-c57af409622c");
        var dto = new TimeEntryDto(
            Id: Guid.Parse("1a20de36-e6ba-48d8-8271-2322461874a6"),
            TaskId: Guid.Parse("d35e54f5-814c-4745-9893-c1c088997f2e"),
            Date: new DateTime(2026, 9, 9),
            Hours: 7.5m,
            Comment: "Completed work",
            EmployeeId: employeeId,
            Status: "Returned");

        var response = new TimeEntryToResponseMapper().Map(dto);

        Assert.Equal(employeeId, response.EmployeeId);
        Assert.Equal("Returned", response.Status);
        var json = JsonSerializer.SerializeToElement(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(employeeId, json.GetProperty("employeeId").GetGuid());
        Assert.Equal("Returned", json.GetProperty("status").GetString());
    }

    private static User CreateUser(string email, UserRole role = UserRole.Employee) =>
        User.Create(PersonName.Create("Test User").Value, EmailAddress.Create(email).Value, role).Value;

    private static TimeEntry CreateEntry(User employee, DateTime date) => TimeEntry.Create(
        employee.UserId,
        TaskId.Create(Guid.Parse("449a0b16-b9d2-4986-b268-0df917874f87")).Value,
        date,
        7.5m,
        "Completed work").Value;

    private static void AssignManager(User employee, User manager, User admin) =>
        Assert.True(employee.AssignManager(manager.UserId, manager.Role, manager.Status,
            admin.Role, admin.Status).IsSuccess);

    private static void Accept(TimeEntry entry, User manager) =>
        Assert.True(entry.Accept(manager.UserId, entry.EmployeeId, manager.UserId,
            manager.Role, manager.Status).IsSuccess);

    private static void AssertOwnerAndStatus(TimeEntry entry, TimeEntryDto dto, string expectedStatus)
    {
        Assert.Equal(entry.EmployeeId.Value, dto.EmployeeId);
        Assert.Equal(expectedStatus, dto.Status);
    }

    private sealed class FakeTimeEntryRepository(IReadOnlyList<User> employees, params TimeEntry[] entries)
        : ITimeEntryRepository
    {
        public Task<TimeEntry?> GetAsync(TimeEntryId id) =>
            Task.FromResult(entries.SingleOrDefault(entry => entry.Id == id));

        public Task<TimeEntry?> GetWithReviewsAsync(TimeEntryId id) => GetAsync(id);

        public Task<IReadOnlyList<TimeEntry>> GetByEmployeeIdAsync(UserId employeeId) =>
            Task.FromResult<IReadOnlyList<TimeEntry>>(entries.Where(entry => entry.EmployeeId == employeeId).ToArray());

        public Task<IReadOnlyList<TimeEntry>> GetByManagerIdAsync(UserId managerId) =>
            Task.FromResult<IReadOnlyList<TimeEntry>>(entries.Where(entry =>
                employees.Any(employee => employee.UserId == entry.EmployeeId && employee.ManagerId == managerId))
                .ToArray());

        public Task<IReadOnlyList<TimeEntry>> GetByDateRangeAsync(DateTime from, DateTime to) =>
            throw new NotSupportedException();

        public Task<bool> ExistsForTaskAsync(TaskId taskId) => throw new NotSupportedException();

        public Task AddAsync(TimeEntry entity) => throw new NotSupportedException();

        public Task RemoveAsync(TimeEntryId id) => throw new NotSupportedException();
    }

    private sealed class FakeUserRepository(params User[] users) : IUserRepository
    {
        public Task<User?> GetAsync(UserId id) =>
            Task.FromResult(users.SingleOrDefault(user => user.UserId == id));

        public Task<User?> GetByEmailAsync(EmailAddress email) => throw new NotSupportedException();

        public Task<IReadOnlyList<User>> GetByManagerIdAsync(UserId managerId) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<User>> GetAllAsync() => throw new NotSupportedException();

        public Task AddAsync(User entity) => throw new NotSupportedException();

        public Task RemoveAsync(UserId id) => throw new NotSupportedException();
    }
}
