using Domain.Aggregate;
using Catagory = Domain.Entities.Catagory;
using EmployeeSystemAccess = Domain.Entities.EmployeeSystemAccess;
using DepartmentEmployee = Domain.Entities.DepartmentEmployee;
using CatagoryReviewer = Domain.Entities.CatagoryReviewer;
using TimeEntryAudit = Domain.Entities.TimeEntryAudit;
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
        Assert.True(titledManager.ChangeProfile(titledManager.Name, titledManager.Email, "Manager").IsSuccess);
        context.Employees.AddRange(reviewer, titledManager);
        Assert.True(reviewer.GrantPermission(SystemPermission.CatagoryReviewer).IsSuccess);
        await context.SaveChangesAsync();

        var access = new EmployeeAccessRepository(context);
        Assert.True((await access.GetWithAccessesAsync(reviewer.Id))!.CanReviewTimeEntries());
        Assert.False((await access.GetWithAccessesAsync(titledManager.Id))!.CanReviewTimeEntries());

        Assert.True(reviewer.RevokePermission(SystemPermission.CatagoryReviewer).IsSuccess);
        await context.SaveChangesAsync();
        Assert.False((await access.GetWithAccessesAsync(reviewer.Id))!.CanReviewTimeEntries());

        Assert.True(reviewer.GrantPermission(SystemPermission.CatagoryReviewer).IsSuccess);
        reviewer.Deactivate();
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
        Assert.True(manager.ChangeProfile(manager.Name, manager.Email, "Manager").IsSuccess);
        var reviewerDepartment = Department.Create(DepartmentName.Create("Reviewer's department").Value).Value;
        var otherDepartment = Department.Create(DepartmentName.Create("Other department").Value).Value;
        var assigned = Category(reviewer.Id, otherDepartment);
        var unrelated = Category(manager.Id, reviewerDepartment);
        var assignedTask = WorkItem(otherDepartment, assigned.Id);
        var unrelatedTask = WorkItem(reviewerDepartment, unrelated.Id);

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
        Assert.Empty(await queue.GetTaskIdsAsync(Array.Empty<CatagoryId>()));
    }

    [Fact]
    public async Task Candidates_include_complete_history_so_a_moved_out_entry_is_not_currently_accessible()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await using var context = await CreateContextAsync(connection);

        var reviewer = Employee("Reviewer");
        var worker = Employee("Worker");
        var otherDepartment = Department.Create(DepartmentName.Create("Worker's department").Value).Value;
        var assigned = Category(reviewer.Id, otherDepartment);
        var unrelated = Category(reviewer.Id, otherDepartment);
        var allowedTask = WorkItem(otherDepartment, assigned.Id);
        var otherTask = WorkItem(otherDepartment, unrelated.Id);

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
        TimeEntry CreateEntry(TaskId taskId, int minute)
        {
            var id = TimeEntryId.Create(Guid.NewGuid()).Value;
            var initial = Audit(id, worker.Id, taskId, firstAuditTime.AddMinutes(minute));
            return PersistedTimeEntry.Create(worker.Id, initial).Value;
        }

        var movedOut = CreateEntry(allowedTask.Id, 0);
        var movedIn = CreateEntry(otherTask.Id, 2);
        var staying = CreateEntry(allowedTask.Id, 4);
        var unrelatedEntry = CreateEntry(otherTask.Id, 6);
        Assert.True(PersistedTimeEntry.Append(movedOut, Audit(movedOut.Id, worker.Id, otherTask.Id, firstAuditTime.AddMinutes(1))).IsSuccess);
        Assert.True(PersistedTimeEntry.Append(movedIn, Audit(movedIn.Id, worker.Id, allowedTask.Id, firstAuditTime.AddMinutes(3))).IsSuccess);

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
            new[] { movedIn.Id.Value, staying.Id.Value }.Order(),
            candidates.Where(entry => tasks.Contains(entry.GetCurrentAudit()!.TaskId))
                .Select(entry => entry.Id.Value).Order());
        Assert.Empty(await queue.GetCandidatesWithAuditsAsync(Array.Empty<TaskId>()));
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

    private static Catagory Category(EmployeeId creator, Department department) =>
        department.AddCatagory(creator, CatagoryName.Create("Test category").Value, false, DateTime.UtcNow).Value;

    private static Domain.Entities.Task WorkItem(Department department, CatagoryId category) =>
        department.AddTask(category, TaskName.Create("Test task").Value, DateTime.UtcNow).Value;

    private static TimeEntryAudit Audit(TimeEntryId entryId, EmployeeId actorId, TaskId taskId, DateTime at) =>
        TimeEntryAudit.Create(entryId, actorId, taskId,
            WorkDate.Create(new DateOnly(2026, 11, 14)).Value, Duration.Create(2m).Value,
            TimeEntryDescription.Create("Work").Value,
            TimeEntryStatus.ReadyForApproval, at).Value;
}
