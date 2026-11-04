using Application.Dtos;
using Application.Handlers;
using Application.Interfaces;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UnitTests.Application.Handlers.Reports;

public class GetTopOrdersHandlerTests
{
    private static readonly DateTime From = new(2026, 10, 1);
    private static readonly DateTime To = new(2026, 10, 31);

    [Fact]
    public async Task EmptyActorId_ReturnsValidationErrorWithoutRepositoryCalls()
    {
        var users = new FakeUserRepository();
        var reports = new FakeTopOrdersRepository();

        AssertFailure("UserIdEmpty", await new GetTopOrdersHandler(reports, users)
            .HandleAsync(new GetTopOrdersQuery(Guid.Empty, From, To)), reports);

        Assert.Empty(users.RequestedIds);
    }

    [Fact]
    public async Task UnknownActor_IsForbiddenWithoutQueryingReport()
    {
        var users = new FakeUserRepository();
        var reports = new FakeTopOrdersRepository();
        var actorId = Guid.NewGuid();

        AssertFailure("ReportForbidden", await new GetTopOrdersHandler(reports, users)
            .HandleAsync(new GetTopOrdersQuery(actorId, From, To)), reports);

        Assert.Equal(actorId, Assert.Single(users.RequestedIds).Value);
    }

    [Theory]
    [InlineData(UserRole.Employee, false)]
    [InlineData(UserRole.Manager, true)]
    [InlineData(UserRole.Admin, true)]
    public async Task EmployeeOrInactiveReviewer_IsForbiddenWithoutQueryingReport(UserRole role, bool inactive)
    {
        var actor = CreateUser(role);
        if (inactive)
            Assert.True(actor.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);
        var users = new FakeUserRepository(actor);
        var reports = new FakeTopOrdersRepository();

        AssertFailure("ReportForbidden", await new GetTopOrdersHandler(reports, users)
            .HandleAsync(new GetTopOrdersQuery(actor.UserId.Value, From, To)), reports);

        Assert.Equal(actor.UserId, Assert.Single(users.RequestedIds));
    }

    [Fact]
    public async Task EndBeforeStart_IsRejectedWithoutQueryingReport()
    {
        var manager = CreateUser(UserRole.Manager);
        var users = new FakeUserRepository(manager);
        var reports = new FakeTopOrdersRepository();

        AssertFailure("InvalidDateRange", await new GetTopOrdersHandler(reports, users)
            .HandleAsync(new GetTopOrdersQuery(manager.UserId.Value, To, From)), reports);

        Assert.Equal(manager.UserId, Assert.Single(users.RequestedIds));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task LimitOutsideOneToTen_IsRejectedWithoutQueryingReport(int limit)
    {
        var admin = CreateUser(UserRole.Admin);
        var reports = new FakeTopOrdersRepository();

        AssertFailure("InvalidLimit", await new GetTopOrdersHandler(reports, new FakeUserRepository(admin))
            .HandleAsync(new GetTopOrdersQuery(admin.UserId.Value, From, To, limit)), reports);
    }

    [Fact]
    public async Task Manager_ForwardsOwnScopeAndReturnsRepositoryRows()
    {
        var manager = CreateUser(UserRole.Manager);
        IReadOnlyList<TopOrdersReportDto> rows =
        [
            new(Guid.NewGuid(), "Repair order", 7.5m,
                [new TopOrderTaskDto(Guid.NewGuid(), "Repair", 7.5m)])
        ];
        var reports = new FakeTopOrdersRepository(rows);
        var users = new FakeUserRepository(manager);
        var query = new GetTopOrdersQuery(manager.UserId.Value, From, To, 1, OnlyAccepted: false);

        var result = await new GetTopOrdersHandler(reports, users).HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        var row = Assert.Single(result.Value);
        Assert.Equal(rows[0].OrderId, row.OrderId);
        Assert.Equal("Repair order", row.OrderName);
        Assert.Equal(7.5m, row.TotalHours);
        var task = Assert.Single(row.Tasks);
        Assert.Equal(rows[0].Tasks[0].TaskId, task.TaskId);
        Assert.Equal("Repair", task.TaskName);
        Assert.Equal(7.5m, task.TotalHours);
        Assert.Equal(manager.UserId, Assert.Single(users.RequestedIds));
        var request = Assert.Single(reports.Requests);
        Assert.Equal(manager.UserId, request.ManagerId);
        Assert.Equal(query.FromDate, request.FromDate);
        Assert.Equal(query.ToDate, request.ToDate);
        Assert.Equal(query.Limit, request.Limit);
        Assert.Equal(query.OnlyAccepted, request.OnlyAccepted);
    }

    [Fact]
    public async Task Admin_QueriesAllOrdersWithDefaultFiltersAndCanReturnEmptyRows()
    {
        var admin = CreateUser(UserRole.Admin);
        IReadOnlyList<TopOrdersReportDto> rows = Array.Empty<TopOrdersReportDto>();
        var reports = new FakeTopOrdersRepository(rows);
        var users = new FakeUserRepository(admin);
        var query = new GetTopOrdersQuery(admin.UserId.Value, From, From); // Equal endpoints and default limit are valid.

        var result = await new GetTopOrdersHandler(reports, users).HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Value);
        Assert.Equal(admin.UserId, Assert.Single(users.RequestedIds));
        var request = Assert.Single(reports.Requests);
        Assert.Null(request.ManagerId);
        Assert.Equal(From, request.FromDate);
        Assert.Equal(From, request.ToDate);
        Assert.Equal(10, request.Limit);
        Assert.True(request.OnlyAccepted);
    }

    private static void AssertFailure(string code, Result result, FakeTopOrdersRepository reports)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, Assert.Single(result.Errors).Code);
        Assert.Empty(reports.Requests);
    }

    private static User CreateUser(UserRole role) => User.Create(
        PersonName.Create("Test User").Value,
        EmailAddress.Create($"{role.ToString().ToLowerInvariant()}@example.com").Value,
        role).Value;

    private sealed class FakeUserRepository(User? user = null) : IUserRepository
    {
        public List<UserId> RequestedIds { get; } = [];

        public Task<User?> GetAsync(UserId id)
        {
            RequestedIds.Add(id);
            return Task.FromResult(user?.UserId == id ? user : null);
        }

        public Task<User?> GetByEmailAsync(EmailAddress email) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> GetByManagerIdAsync(UserId managerId) => throw new NotSupportedException();
        public Task<IReadOnlyList<User>> GetAllAsync() => throw new NotSupportedException();
        public Task AddAsync(User entity) => throw new NotSupportedException();
        public Task RemoveAsync(UserId id) => throw new NotSupportedException();
    }

    private sealed class FakeTopOrdersRepository(IReadOnlyList<TopOrdersReportDto>? rows = null) : ITopOrdersRepository
    {
        public List<ReportRequest> Requests { get; } = [];

        public Task<IReadOnlyList<TopOrdersReportDto>> GetTopOrdersAsync(UserId? managerId,
            DateTime fromDate, DateTime toDate, int limit, bool onlyAccepted,
            CancellationToken cancellationToken = default)
        {
            if (rows is null)
                throw new InvalidOperationException("Unexpected report query.");

            Requests.Add(new(managerId, fromDate, toDate, limit, onlyAccepted));
            return Task.FromResult(rows);
        }
    }

    private sealed record ReportRequest(UserId? ManagerId, DateTime FromDate, DateTime ToDate,
        int Limit, bool OnlyAccepted);
}
