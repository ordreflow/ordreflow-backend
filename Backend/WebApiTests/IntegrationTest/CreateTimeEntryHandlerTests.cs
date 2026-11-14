using Application.Commands;
using Application.Handlers;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;
using WorkTask = Domain.Entities.Task;
using TaskStatus = Domain.ValueObjects.TaskStatus;

namespace WebApiTests.IntegrationTest;

public sealed class CreateTimeEntryHandlerTests
{
    [Fact]
    public async Task Missing_employee_permission_stops_before_work_lookups()
    {
        var employee = new Employee
        {
            Id = EmployeeId.Create(Guid.NewGuid()).Value,
            Name = PersonName.Create("Worker").Value,
            Email = EmailAddress.Create("worker@test.invalid").Value,
            Status = EmployeeStatus.Active, CreatedAt = DateTime.UtcNow
        };
        var work = Work();
        var handler = Handler(employee, work);

        var result = await handler.HandleAsync(Command(employee.Id, work.WorkItem.Id));

        Assert.Equal("EmployeeAccessForbidden", Assert.Single(result.Errors).Code);
        Assert.Equal(0, work.TaskLookups);
        Assert.Null(work.Added);
    }

    [Fact]
    public async Task Active_employee_creates_draft_entry_with_initial_utc_audit_on_general_task()
    {
        var employee = Employee();
        var work = Work();
        var handler = Handler(employee, work);

        var result = await handler.HandleAsync(Command(employee.Id, work.WorkItem.Id) with { Comment = null });

        Assert.True(result.IsSuccess);
        var entry = Assert.IsType<TimeEntry>(work.Added);
        Assert.Equal(employee.Id, entry.EmployeeId);
        Assert.Equal(1, entry.Revision);
        var audit = Assert.Single(entry.Audits);
        Assert.Equal(entry.Id, audit.TimeEntryId);
        Assert.Equal(employee.Id, audit.ActorId);
        Assert.Equal(work.WorkItem.Id, audit.TaskId);
        Assert.Equal(new DateOnly(2026, 11, 14), audit.WorkDate.Value);
        Assert.Equal(2.5m, audit.Duration.Value);
        Assert.Equal(string.Empty, audit.Description.Value);
        Assert.Equal(TimeEntryStatus.Draft, audit.Status);
        Assert.Equal(DateTimeKind.Utc, audit.AuditTime.Kind);
        Assert.Equal(0, work.MembershipLookups);
    }

    [Fact]
    public async Task Internal_category_requires_membership_even_for_assigned_employee()
    {
        var employee = Employee();
        var work = Work(internalOnly: true);
        work.Assignment = new TaskAssignmentAccess(true, true);
        var handler = Handler(employee, work);

        var denied = await handler.HandleAsync(Command(employee.Id, work.WorkItem.Id));
        Assert.Equal("CatagoryAccessForbidden", Assert.Single(denied.Errors).Code);
        Assert.Null(work.Added);

        work.IsDepartmentMember = true;
        var allowed = await handler.HandleAsync(Command(employee.Id, work.WorkItem.Id));
        Assert.True(allowed.IsSuccess);
        Assert.NotNull(work.Added);
    }

    [Fact]
    public async Task Assigned_task_rejects_nonassignees_but_accepts_an_assignee()
    {
        var employee = Employee();
        var work = Work();
        work.Assignment = new TaskAssignmentAccess(true, false);
        var handler = Handler(employee, work);

        var denied = await handler.HandleAsync(Command(employee.Id, work.WorkItem.Id));
        Assert.Equal("TaskAssignmentForbidden", Assert.Single(denied.Errors).Code);
        Assert.Null(work.Added);

        work.Assignment = new TaskAssignmentAccess(true, true);
        Assert.True((await handler.HandleAsync(Command(employee.Id, work.WorkItem.Id))).IsSuccess);
        Assert.NotNull(work.Added);
    }

    [Theory]
    [InlineData(true, "CatagoryClosed")]
    [InlineData(false, "TaskClosed")]
    public async Task Closed_work_is_not_registerable(bool closeCategory, string expectedError)
    {
        var employee = Employee();
        var work = Work();
        if (closeCategory)
            work.Catagory.Status = CatagoryStatus.Closed;
        else
            work.WorkItem.Status = TaskStatus.Closed;

        var result = await Handler(employee, work).HandleAsync(Command(employee.Id, work.WorkItem.Id));

        Assert.Equal(expectedError, Assert.Single(result.Errors).Code);
        Assert.Null(work.Added);
    }

    [Fact]
    public async Task Invalid_duration_is_rejected_before_work_lookups()
    {
        var employee = Employee();
        var work = Work();

        var result = await Handler(employee, work).HandleAsync(Command(employee.Id, work.WorkItem.Id) with { Hours = 25 });

        Assert.Equal("InvalidDuration", Assert.Single(result.Errors).Code);
        Assert.Equal(0, work.TaskLookups);
    }

    [Fact]
    public async Task Non_quarter_duration_is_rejected_before_work_lookups()
    {
        var employee = Employee();
        var work = Work();

        var result = await Handler(employee, work).HandleAsync(
            Command(employee.Id, work.WorkItem.Id) with { Hours = 7.41m });

        Assert.Equal("InvalidDurationIncrement", Assert.Single(result.Errors).Code);
        Assert.Equal(0, work.TaskLookups);
        Assert.Null(work.Added);
    }

    [Fact]
    public async Task Full_and_half_day_exceptions_are_recorded_without_rounding()
    {
        var employee = Employee();
        var work = Work();
        var handler = Handler(employee, work);

        foreach (var hours in new[] { 7.4m, 3.7m })
        {
            var result = await handler.HandleAsync(Command(employee.Id, work.WorkItem.Id) with { Hours = hours });

            Assert.True(result.IsSuccess);
            Assert.Equal(hours, work.Added!.GetCurrentAudit()!.Duration.Value);
        }
    }

    private static CreateTimeEntryHandler Handler(Employee employee, FakeRegistrationRepository work) =>
        new(new FakeEmployeeRepository(employee), work, new FixedTimeProvider());

    private static CreateTimeEntryCommand Command(EmployeeId employeeId, TaskId taskId) =>
        new(employeeId.Value, taskId.Value, new DateTime(2026, 11, 14), 2.5m, "Work");

    private static Employee Employee() =>
        Domain.Aggregate.Employee.Create("Worker", $"{Guid.NewGuid():N}@test.invalid", DateTime.UtcNow).Value;

    private static FakeRegistrationRepository Work(bool internalOnly = false)
    {
        var category = new Catagory
        {
            Id = CatagoryId.Create(Guid.NewGuid()).Value,
            DepartmentId = DepartmentId.Create(Guid.NewGuid()).Value,
            Name = CatagoryName.Create("Category").Value,
            Status = CatagoryStatus.Active, InternalOnly = internalOnly
        };
        return new FakeRegistrationRepository(new WorkTask
        {
            Id = TaskId.Create(Guid.NewGuid()).Value,
            CatagoryId = category.Id, Name = TaskName.Create("Task").Value, Status = TaskStatus.Active
        }, category);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeEmployeeRepository(Employee employee) : IEmployeeAccessRepository
    {
        public Task<Employee?> GetWithAccessesAsync(EmployeeId employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Employee?>(employee.Id == employeeId ? employee : null);
    }

    private sealed class FakeRegistrationRepository(WorkTask task, Catagory catagory) : ITimeRegistrationRepository
    {
        public WorkTask WorkItem { get; } = task;
        public Catagory Catagory { get; } = catagory;
        public TaskAssignmentAccess Assignment { get; set; }
        public bool IsDepartmentMember { get; set; }
        public int TaskLookups { get; private set; }
        public int MembershipLookups { get; private set; }
        public TimeEntry? Added { get; private set; }

        public Task<WorkTask?> GetTaskAsync(TaskId taskId, CancellationToken cancellationToken = default)
        {
            TaskLookups++;
            return Task.FromResult<WorkTask?>(taskId == WorkItem.Id ? WorkItem : null);
        }

        public Task<Catagory?> GetCatagoryAsync(CatagoryId catagoryId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Catagory?>(catagoryId == Catagory.Id ? Catagory : null);

        public Task<bool> IsDepartmentMemberAsync(
            EmployeeId employeeId, DepartmentId departmentId, CancellationToken cancellationToken = default)
        {
            MembershipLookups++;
            return Task.FromResult(IsDepartmentMember);
        }

        public Task<TaskAssignmentAccess> GetAssignmentAccessAsync(
            TaskId taskId, EmployeeId employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Assignment);

        public Task AddAsync(TimeEntry entry, CancellationToken cancellationToken = default)
        {
            Added = entry;
            return Task.CompletedTask;
        }
    }
}
