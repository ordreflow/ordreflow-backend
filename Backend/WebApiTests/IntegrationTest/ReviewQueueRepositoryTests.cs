using Domain.Aggregate;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Repositories;
using TaskStatus = Domain.ValueObjects.TaskStatus;

namespace WebApiTests.IntegrationTest;

public sealed class ReviewQueueRepositoryTests
{
    [Fact]
    public async Task New_employee_starts_with_only_persisted_employee_permission()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await using var context = await CreateContextAsync(connection);
        var employee = Employee("New employee");
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var stored = await new EmployeeAccessRepository(context).GetWithAccessesAsync(employee.Id);

        Assert.NotNull(stored);
        Assert.True(stored.HasPermission(SystemPermission.Employee));
        Assert.False(stored.HasPermission(SystemPermission.CatagoryReviewer));
        Assert.Single(stored.Accesses);
    }

    [Fact]
    public async Task Access_lookup_checks_current_permission_and_active_status_instead_of_title()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await using var context = await CreateContextAsync(connection);

        var reviewer = Employee("Reviewer");
        var titledManager = Employee("Manager");
        titledManager.Title = "Manager";
        context.Employees.AddRange(reviewer, titledManager);
        context.EmployeeSystemAccesses.Add(new EmployeeSystemAccess
        {
            EmployeeId = reviewer.Id,
            Permission = SystemPermission.CatagoryReviewer
        });
        await context.SaveChangesAsync();

        var access = new EmployeeAccessRepository(context);
        Assert.True((await access.GetWithAccessesAsync(reviewer.Id))!.CanReviewTimeEntries());
        Assert.False((await access.GetWithAccessesAsync(titledManager.Id))!.CanReviewTimeEntries());

        var grant = await context.EmployeeSystemAccesses.SingleAsync(
            access => access.Permission == SystemPermission.CatagoryReviewer);
        context.EmployeeSystemAccesses.Remove(grant);
        await context.SaveChangesAsync();
        Assert.False((await access.GetWithAccessesAsync(reviewer.Id))!.CanReviewTimeEntries());

        context.EmployeeSystemAccesses.Add(new EmployeeSystemAccess
        {
            EmployeeId = reviewer.Id,
            Permission = SystemPermission.CatagoryReviewer
        });
        reviewer.Status = EmployeeStatus.Deactivated;
        await context.SaveChangesAsync();
        Assert.False((await access.GetWithAccessesAsync(reviewer.Id))!.CanReviewTimeEntries());
    }

    [Fact]
    public async Task Assigned_categories_select_exact_tasks_even_across_departments()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await using var context = await CreateContextAsync(connection);

        var reviewer = Employee("Reviewer");
        var manager = Employee("Manager");
        manager.Title = "Manager";
        var reviewerDepartment = new Department { Id = Guid.NewGuid(), Name = "Reviewer's department" };
        var otherDepartment = new Department { Id = Guid.NewGuid(), Name = "Other department" };
        var assigned = Category(reviewer.Id, otherDepartment.Id);
        var unrelated = Category(manager.Id, reviewerDepartment.Id);
        var assignedTask = WorkItem(assigned.Id);
        var unrelatedTask = WorkItem(unrelated.Id);

        context.Employees.AddRange(reviewer, manager);
        context.Departments.AddRange(reviewerDepartment, otherDepartment);
        context.Catagories.AddRange(assigned, unrelated);
        context.Tasks.AddRange(assignedTask, unrelatedTask);
        context.DepartmentEmployees.Add(new DepartmentEmployee
        {
            EmployeeId = reviewer.Id,
            DepartmentId = reviewerDepartment.Id
        });
        context.CatagoryReviewers.Add(new CatagoryReviewer
        {
            EmployeeId = reviewer.Id,
            CatagoryId = assigned.Id
        });
        await context.SaveChangesAsync();

        var queue = new ReviewQueueRepository(context);
        var categoryIds = await queue.GetAssignedCatagoryIdsAsync(reviewer.Id);
        Assert.Equal(assigned.Id, Assert.Single(categoryIds));
        Assert.Equal(assignedTask.Id, Assert.Single(await queue.GetTaskIdsAsync(categoryIds)));
        Assert.Empty(await queue.GetAssignedCatagoryIdsAsync(manager.Id));
        Assert.Empty(await queue.GetTaskIdsAsync(Array.Empty<Guid>()));
    }

    [Fact]
    public async Task Candidates_include_complete_history_so_a_moved_out_entry_is_not_currently_accessible()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await using var context = await CreateContextAsync(connection);

        var reviewer = Employee("Reviewer");
        var worker = Employee("Worker");
        var otherDepartment = new Department { Id = Guid.NewGuid(), Name = "Worker's department" };
        var assigned = Category(reviewer.Id, otherDepartment.Id);
        var unrelated = Category(reviewer.Id, otherDepartment.Id);
        var allowedTask = WorkItem(assigned.Id);
        var otherTask = WorkItem(unrelated.Id);

        context.Employees.AddRange(reviewer, worker);
        context.Departments.Add(otherDepartment);
        context.Catagories.AddRange(assigned, unrelated);
        context.Tasks.AddRange(allowedTask, otherTask);
        context.CatagoryReviewers.Add(new CatagoryReviewer
        {
            EmployeeId = reviewer.Id,
            CatagoryId = assigned.Id
        });

        var firstAuditTime = new DateTime(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc);
        TimeEntry CreateEntry(Guid taskId, int minute)
        {
            var id = Guid.NewGuid();
            var initial = Audit(id, worker.Id, taskId, firstAuditTime.AddMinutes(minute));
            return TimeEntry.Create(worker.Id, initial).Value;
        }

        var movedOut = CreateEntry(allowedTask.Id, 0);
        var movedIn = CreateEntry(otherTask.Id, 2);
        var staying = CreateEntry(allowedTask.Id, 4);
        var unrelatedEntry = CreateEntry(otherTask.Id, 6);
        Assert.True(movedOut.AddAudit(Audit(movedOut.Id, worker.Id, otherTask.Id, firstAuditTime.AddMinutes(1))).IsSuccess);
        Assert.True(movedIn.AddAudit(Audit(movedIn.Id, worker.Id, allowedTask.Id, firstAuditTime.AddMinutes(3))).IsSuccess);

        context.TimeEntries.AddRange(movedOut, movedIn, staying, unrelatedEntry);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var queue = new ReviewQueueRepository(context);
        var tasks = await queue.GetTaskIdsAsync(await queue.GetAssignedCatagoryIdsAsync(reviewer.Id));
        var candidates = await queue.GetCandidatesWithAuditsAsync(tasks);

        Assert.Equal(3, candidates.Count);
        Assert.Equal(2, candidates.Single(entry => entry.Id == movedOut.Id).Audits.Count);
        Assert.Equal(2, candidates.Single(entry => entry.Id == movedIn.Id).Audits.Count);
        Assert.Equal(
            new[] { movedIn.Id, staying.Id }.Order(),
            candidates.Where(entry => tasks.Contains(entry.GetCurrentAudit()!.TaskId))
                .Select(entry => entry.Id).Order());
        Assert.Empty(await queue.GetCandidatesWithAuditsAsync(Array.Empty<Guid>()));
    }

    private static async Task<AppDbContext> CreateContextAsync(SqliteConnection connection)
    {
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static Employee Employee(string name) =>
        Domain.Aggregate.Employee.Create(name, $"{Guid.NewGuid():N}@test.invalid", DateTime.UtcNow).Value;

    private static Catagory Category(Guid creatorId, Guid departmentId) => new()
    {
        Id = Guid.NewGuid(),
        CreatedBy = creatorId,
        DepartmentId = departmentId,
        Name = "Test category",
        Status = CatagoryStatus.Active,
        CreatedAt = DateTime.UtcNow
    };

    private static Domain.Entities.Task WorkItem(Guid catagoryId) => new()
    {
        Id = Guid.NewGuid(),
        CatagoryId = catagoryId,
        Name = "Test task",
        Status = TaskStatus.Active,
        CreatedAt = DateTime.UtcNow
    };

    private static TimeEntryAudit Audit(Guid entryId, Guid actorId, Guid taskId, DateTime at) =>
        TimeEntryAudit.Create(entryId, actorId, taskId, new DateOnly(2026, 11, 14), 2, "Work",
            TimeEntryStatus.ReadyForApproval, at).Value;
}
