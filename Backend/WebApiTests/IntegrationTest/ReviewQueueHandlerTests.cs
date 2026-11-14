using Application.Handlers;
using Application.Queries;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Repositories;
using TimeEntryAudit = Domain.Entities.TimeEntryAudit;

namespace WebApiTests.IntegrationTest;

public sealed class ReviewQueueHandlerTests
{
    private static readonly DateTime FirstAuditTime = new(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Empty_reviewer_id_fails_before_loading_employee_or_queue()
    {
        var queue = new FakeReviewQueueRepository();
        var handler = new GetReviewerTimeEntriesHandler(new UnexpectedEmployeeAccessRepository(), queue);

        var result = await handler.HandleAsync(new GetReviewerTimeEntriesQuery(Guid.Empty, null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("EmployeeIdEmpty", Assert.Single(result.Errors).Code);
        Assert.Equal(0, queue.CatagoryLookups);
    }

    [Theory]
    [InlineData(false, EmployeeStatus.Active)]
    [InlineData(true, EmployeeStatus.Deactivated)]
    public async Task Reviewer_permission_and_active_status_are_required_before_scope_lookups(
        bool grantPermission, EmployeeStatus status)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await using var context = await CreateContextAsync(connection);
        var reviewerId = await AddReviewerAsync(context, grantPermission, status);
        var queue = new FakeReviewQueueRepository();
        var handler = new GetReviewerTimeEntriesHandler(new EmployeeAccessRepository(context), queue);

        var result = await handler.HandleAsync(new GetReviewerTimeEntriesQuery(reviewerId, null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("ReviewForbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(0, queue.CatagoryLookups);
        Assert.Equal(0, queue.TaskLookups);
        Assert.Equal(0, queue.EntryLookups);
    }

    [Fact]
    public async Task Missing_reviewer_fails_before_scope_lookups()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await using var context = await CreateContextAsync(connection);
        var queue = new FakeReviewQueueRepository();
        var handler = new GetReviewerTimeEntriesHandler(new EmployeeAccessRepository(context), queue);

        var result = await handler.HandleAsync(new GetReviewerTimeEntriesQuery(Guid.NewGuid(), null, null, null));

        Assert.True(result.IsFailure);
        Assert.Equal("ReviewForbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(0, queue.CatagoryLookups);
    }

    [Fact]
    public async Task No_assigned_categories_returns_empty_without_fetching_tasks_or_entries()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await using var context = await CreateContextAsync(connection);
        var reviewerId = await AddReviewerAsync(context);
        var queue = new FakeReviewQueueRepository();
        var handler = new GetReviewerTimeEntriesHandler(new EmployeeAccessRepository(context), queue);

        var result = await handler.HandleAsync(new GetReviewerTimeEntriesQuery(reviewerId, null, null, null));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
        Assert.Equal(1, queue.CatagoryLookups);
        Assert.Equal(0, queue.TaskLookups);
        Assert.Equal(0, queue.EntryLookups);
    }

    [Fact]
    public async Task No_tasks_returns_empty_without_fetching_entries()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await using var context = await CreateContextAsync(connection);
        var reviewerId = await AddReviewerAsync(context);
        var queue = new FakeReviewQueueRepository { CatagoryIds = [NewCatagoryId()] };
        var handler = new GetReviewerTimeEntriesHandler(new EmployeeAccessRepository(context), queue);

        var result = await handler.HandleAsync(new GetReviewerTimeEntriesQuery(reviewerId, null, null, null));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
        Assert.Equal(1, queue.TaskLookups);
        Assert.Equal(0, queue.EntryLookups);
    }

    [Fact]
    public async Task Current_audit_must_be_ready_and_on_an_assigned_task()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await using var context = await CreateContextAsync(connection);
        var reviewerId = await AddReviewerAsync(context);
        var employeeId = NewEmployeeId(); // The entry owner need not be in the reviewer's department.
        var authorizedTaskId = NewTaskId();
        var otherTaskId = NewTaskId();
        var workDate = new DateOnly(2026, 11, 14);

        var movedOut = Entry(employeeId, authorizedTaskId, workDate, TimeEntryStatus.ReadyForApproval);
        AddAudit(movedOut, otherTaskId, workDate, TimeEntryStatus.ReadyForApproval);
        var movedIn = Entry(employeeId, otherTaskId, workDate, TimeEntryStatus.Approved);
        AddAudit(movedIn, authorizedTaskId, workDate, TimeEntryStatus.ReadyForApproval);
        var approved = Entry(employeeId, authorizedTaskId, workDate, TimeEntryStatus.ReadyForApproval);
        AddAudit(approved, authorizedTaskId, workDate, TimeEntryStatus.Approved);
        var draft = Entry(employeeId, authorizedTaskId, workDate, TimeEntryStatus.Draft);

        var queue = new FakeReviewQueueRepository
        {
            CatagoryIds = [NewCatagoryId()],
            TaskIds = [authorizedTaskId],
            Candidates = [approved, movedOut, draft, movedIn]
        };
        var handler = new GetReviewerTimeEntriesHandler(new EmployeeAccessRepository(context), queue);

        var result = await handler.HandleAsync(new GetReviewerTimeEntriesQuery(reviewerId, null, null, null));

        Assert.True(result.IsSuccess);
        var row = Assert.Single(result.Value);
        Assert.Equal(movedIn.Id.Value, row.Id);
        Assert.Equal(authorizedTaskId.Value, row.TaskId);
        Assert.Equal(employeeId.Value, row.EmployeeId);
        Assert.Equal(workDate.ToDateTime(TimeOnly.MinValue), row.Date);
        Assert.Equal(2.25m, row.Hours);
        Assert.Equal("Latest version", row.Comment);
        Assert.Equal("ReadyForApproval", row.Status);
        Assert.Equal(1, queue.EntryLookups);
    }

    [Fact]
    public async Task Date_range_uses_current_work_date_with_exclusive_upper_bound()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await using var context = await CreateContextAsync(connection);
        var reviewerId = await AddReviewerAsync(context);
        var employeeId = NewEmployeeId();
        var taskId = NewTaskId();
        var start = new DateOnly(2026, 11, 14);
        var endExclusive = start.AddDays(2);
        var before = Entry(employeeId, taskId, start.AddDays(-1), TimeEntryStatus.ReadyForApproval);
        var onStart = Entry(employeeId, taskId, start, TimeEntryStatus.ReadyForApproval);
        var inside = Entry(employeeId, taskId, start.AddDays(1), TimeEntryStatus.ReadyForApproval);
        var onEnd = Entry(employeeId, taskId, endExclusive, TimeEntryStatus.ReadyForApproval);
        var changedDate = Entry(employeeId, taskId, start.AddDays(-1), TimeEntryStatus.ReadyForApproval);
        AddAudit(changedDate, taskId, start.AddDays(1), TimeEntryStatus.ReadyForApproval);
        var queue = new FakeReviewQueueRepository
        {
            CatagoryIds = [NewCatagoryId()],
            TaskIds = [taskId],
            Candidates = [onEnd, inside, before, changedDate, onStart]
        };
        var handler = new GetReviewerTimeEntriesHandler(new EmployeeAccessRepository(context), queue);

        var result = await handler.HandleAsync(new GetReviewerTimeEntriesQuery(
            reviewerId, start.ToDateTime(TimeOnly.MinValue), endExclusive.ToDateTime(TimeOnly.MinValue), null));

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { onStart.Id.Value, inside.Id.Value, changedDate.Id.Value }.Order(),
            result.Value.Select(row => row.Id).Order());
        Assert.Equal(start.ToDateTime(TimeOnly.MinValue), result.Value[0].Date);
        Assert.All(result.Value.Skip(1), row => Assert.Equal(start.AddDays(1).ToDateTime(TimeOnly.MinValue), row.Date));
    }

    [Fact]
    public async Task Explicit_status_filter_cannot_broaden_review_queue()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await using var context = await CreateContextAsync(connection);
        var reviewerId = await AddReviewerAsync(context);
        var queue = new FakeReviewQueueRepository { CatagoryIds = [NewCatagoryId()] };
        var handler = new GetReviewerTimeEntriesHandler(new EmployeeAccessRepository(context), queue);

        var result = await handler.HandleAsync(new GetReviewerTimeEntriesQuery(
            reviewerId, null, null, TimeEntryStatus.Rejected));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
        Assert.Equal(0, queue.CatagoryLookups);
    }

    private static async Task<AppDbContext> CreateContextAsync(SqliteConnection connection)
    {
        await connection.OpenAsync();
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static async Task<Guid> AddReviewerAsync(
        AppDbContext context, bool grantPermission = true, EmployeeStatus status = EmployeeStatus.Active)
    {
        var employee = Employee.Create("Reviewer", $"{Guid.NewGuid():N}@test.invalid", DateTime.UtcNow).Value;
        if (grantPermission)
            Assert.True(employee.GrantPermission(SystemPermission.CatagoryReviewer).IsSuccess);
        employee.Status = status;
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        return employee.Id.Value;
    }

    private static EmployeeId NewEmployeeId() => EmployeeId.Create(Guid.NewGuid()).Value;
    private static TaskId NewTaskId() => TaskId.Create(Guid.NewGuid()).Value;
    private static CatagoryId NewCatagoryId() => CatagoryId.Create(Guid.NewGuid()).Value;

    private static TimeEntry Entry(EmployeeId employeeId, TaskId taskId, DateOnly workDate, TimeEntryStatus status)
    {
        var id = TimeEntryId.Create(Guid.NewGuid()).Value;
        return TimeEntry.Create(employeeId,
            TimeEntryAudit.Create(id, employeeId, taskId, WorkDate.Create(workDate).Value,
                Duration.Create(1.5m).Value, TimeEntryDescription.Create("Initial version").Value,
                status, FirstAuditTime).Value).Value;
    }

    private static void AddAudit(TimeEntry entry, TaskId taskId, DateOnly workDate, TimeEntryStatus status) =>
        Assert.True(entry.AddAudit(TimeEntryAudit.Create(entry.Id, entry.EmployeeId, taskId,
            WorkDate.Create(workDate).Value, Duration.Create(2.25m).Value,
            TimeEntryDescription.Create("Latest version").Value, status, FirstAuditTime.AddMinutes(1)).Value).IsSuccess);

    private sealed class UnexpectedEmployeeAccessRepository : IEmployeeAccessRepository
    {
        public Task<Employee?> GetWithAccessesAsync(EmployeeId employeeId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("An empty reviewer ID must not trigger a database lookup.");
    }

    private sealed class FakeReviewQueueRepository : IReviewQueueRepository
    {
        public IReadOnlyList<CatagoryId> CatagoryIds { get; init; } = [];
        public IReadOnlyList<TaskId> TaskIds { get; init; } = [];
        public IReadOnlyList<TimeEntry> Candidates { get; init; } = [];
        public int CatagoryLookups { get; private set; }
        public int TaskLookups { get; private set; }
        public int EntryLookups { get; private set; }

        public Task<IReadOnlyList<CatagoryId>> GetAssignedCatagoryIdsAsync(
            EmployeeId employeeId, CancellationToken cancellationToken = default)
        {
            CatagoryLookups++;
            return Task.FromResult(CatagoryIds);
        }

        public Task<IReadOnlyList<TaskId>> GetTaskIdsAsync(
            IReadOnlyCollection<CatagoryId> catagoryIds, CancellationToken cancellationToken = default)
        {
            TaskLookups++;
            return Task.FromResult(TaskIds);
        }

        public Task<IReadOnlyList<TimeEntry>> GetCandidatesWithAuditsAsync(
            IReadOnlyCollection<TaskId> taskIds, CancellationToken cancellationToken = default)
        {
            EntryLookups++;
            return Task.FromResult(Candidates);
        }
    }
}
