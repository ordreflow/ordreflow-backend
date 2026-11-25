using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Queries;
using Catagory = Domain.Entities.Catagory;
using DepartmentManager = Domain.Entities.DepartmentManager;
using DepartmentEmployee = Domain.Entities.DepartmentEmployee;
using TimeEntryAudit = Domain.Entities.TimeEntryAudit;
using WorkTask = Domain.Entities.Task;

namespace WebApiTests.IntegrationTest;

public sealed class TopCategoriesQueryTests
{
    private static readonly DateOnly From = new(2026, 11, 1);
    private static readonly DateOnly To = new(2026, 11, 30);
    private static readonly DateTime Created = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Totals_include_all_non_deleted_statuses_inclusive_dates_and_cross_department_employees()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = Context(connection);
        var world = await World.CreateAsync(context);
        var category = world.Category(world.Owned, "Main");
        var first = world.Task(world.Owned, category, "First");
        var second = world.Task(world.Owned, category, "Second");
        world.Task(world.Owned, category, "No registered hours");
        world.Entry(first, From, 1m, TimeEntryStatus.Draft);
        world.Entry(first, To, 2m, TimeEntryStatus.ReadyForApproval);
        world.Entry(second, From.AddDays(1), 3m, TimeEntryStatus.Rejected);
        world.Entry(second, From.AddDays(2), 4m, TimeEntryStatus.Approved);
        world.Entry(second, From.AddDays(3), 5m, TimeEntryStatus.Closed);
        world.Entry(first, From.AddDays(4), 24m, TimeEntryStatus.Deleted);
        world.Entry(first, From.AddDays(-1), 24m);
        world.Entry(first, To.AddDays(1), 24m);
        var smaller = world.Category(world.Owned, "Smaller");
        world.Entry(world.Task(world.Owned, smaller, "Other"), From, 2m);
        var foreign = world.Category(world.Other, "Foreign");
        var foreignTask = world.Task(world.Other, foreign, "Private work");
        world.Entry(foreignTask, From, 24m);
        world.Entry(foreignTask, From, 24m);
        // The worker belongs to another department and is now inactive; the owning
        // category/department, not employee membership/status, determines report scope.
        world.Worker.Deactivate();
        Assert.True(world.Owned.CloseTask(category.Id, second.Id, Created.AddDays(1)).IsSuccess);
        Assert.True(world.Owned.CloseCatagory(category.Id, Created.AddDays(1)).IsSuccess);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var query = new TopCategoriesQuery(context);

        var scoped = await query.GetAsync(world.Manager.Id, From, To);
        var all = await query.GetAsync(null, From, To);

        Assert.Equal(2, scoped.Count);
        Assert.Equal(category.Id.Value, scoped[0].CategoryId);
        Assert.Equal(world.Owned.Id.Value, scoped[0].DepartmentId);
        Assert.Equal(15m, scoped[0].TotalHours);
        Assert.Equal(new[] { second.Id.Value, first.Id.Value }, scoped[0].Tasks.Select(task => task.TaskId));
        Assert.Equal(new[] { 12m, 3m }, scoped[0].Tasks.Select(task => task.TotalHours));
        Assert.Equal(scoped[0].TotalHours, scoped[0].Tasks.Sum(task => task.TotalHours));
        Assert.Equal(foreign.Id.Value, all[0].CategoryId);
        Assert.Equal(48m, all[0].TotalHours);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Latest_audit_is_selected_before_date_deletion_and_scope_filters()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = Context(connection);
        var world = await World.CreateAsync(context);
        var category = world.Category(world.Owned, "Owned");
        var task = world.Task(world.Owned, category, "Owned task");
        var foreignCategory = world.Category(world.Other, "Foreign");
        var foreign = world.Task(world.Other, foreignCategory, "Foreign task");
        var edited = world.Entry(task, From, 2m);
        Version(edited, task, To, 7.4m, TimeEntryStatus.Rejected);
        var movedDateOut = world.Entry(task, From, 24m);
        Version(movedDateOut, task, To.AddDays(1), 24m, TimeEntryStatus.ReadyForApproval);
        var deletedOutside = world.Entry(task, From, 24m);
        Version(deletedOutside, task, To.AddDays(1), 24m, TimeEntryStatus.Deleted);
        var movedDateIn = world.Entry(task, From.AddDays(-1), 24m);
        Version(movedDateIn, task, From.AddDays(1), 3.7m, TimeEntryStatus.Draft);
        var movedScopeOut = world.Entry(task, From, 24m);
        Version(movedScopeOut, foreign, From, 24m, TimeEntryStatus.Approved);
        var movedScopeIn = world.Entry(foreign, From, 24m);
        Version(movedScopeIn, task, From, 1m, TimeEntryStatus.ReadyForApproval);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var query = new TopCategoriesQuery(context);

        var scoped = await query.GetAsync(world.Manager.Id, From, To);
        var all = await query.GetAsync(null, From, To);

        Assert.Equal(12.1m, Assert.Single(scoped).TotalHours);
        Assert.Equal(12.1m, Assert.Single(scoped[0].Tasks).TotalHours);
        Assert.Equal(2, all.Count);
        Assert.Equal(24m, all.Single(item => item.CategoryId == foreignCategory.Id.Value).TotalHours);
        Assert.Equal(12.1m, all.Single(item => item.CategoryId == category.Id.Value).TotalHours);
    }

    [Fact]
    public async Task Ranking_is_top_ten_categories_with_all_qualifying_tasks_and_stable_ties()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = Context(connection);
        var world = await World.CreateAsync(context);
        var expected = new List<(Guid Id, decimal Hours)>();
        Catagory? tiedTasks = null;
        for (var index = 1; index <= 12; index++)
        {
            // Identical names must not merge distinct category identities.
            var category = world.Category(world.Owned, "Same label");
            var task = world.Task(world.Owned, category, "First");
            var hours = index == 12 ? 6m : index == 11 ? 5m : index;
            world.Entry(task, From, hours);
            if (index >= 11)
            {
                var extra = index == 12 ? 6m : 7m;
                world.Entry(world.Task(world.Owned, category, "Second"), From, extra);
                hours += extra;
            }
            if (index == 12) tiedTasks = category;
            expected.Add((category.Id.Value, hours));
        }
        world.Category(world.Owned, "No entries");
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var query = new TopCategoriesQuery(context);

        var first = await query.GetAsync(world.Manager.Id, From, To);
        var again = await query.GetAsync(world.Manager.Id, From, To);

        Assert.Equal(10, first.Count);
        Assert.Equal(expected.OrderByDescending(item => item.Hours).ThenBy(item => item.Id).Take(10).Select(item => item.Id),
            first.Select(item => item.CategoryId));
        Assert.Equal(first.Select(item => item.CategoryId), again.Select(item => item.CategoryId));
        Assert.All(first, category => Assert.Equal(category.TotalHours, category.Tasks.Sum(task => task.TotalHours)));
        var breakdown = first.Single(item => item.CategoryId == tiedTasks!.Id.Value);
        Assert.Equal(2, breakdown.Tasks.Count);
        Assert.Equal(breakdown.Tasks.Select(task => task.TaskId).Order(), breakdown.Tasks.Select(task => task.TaskId));
    }

    [Fact]
    public async Task Equal_totals_at_the_top_ten_cutoff_use_category_identity_for_a_stable_selection()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = Context(connection);
        var world = await World.CreateAsync(context);
        var ids = new List<Guid>();
        for (var index = 0; index < 12; index++)
        {
            var category = world.Category(world.Owned, "Equal hours");
            ids.Add(category.Id.Value);
            world.Entry(world.Task(world.Owned, category, "Task"), From, 1m);
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var query = new TopCategoriesQuery(context);

        var result = await query.GetAsync(world.Manager.Id, From, To);

        Assert.Equal(ids.Order().Take(10), result.Select(item => item.CategoryId));
        Assert.All(result, item => Assert.Equal(1m, item.TotalHours));
    }

    [Fact]
    public async Task Multiple_managed_departments_do_not_duplicate_hours_and_revoked_links_take_effect()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = Context(connection);
        var world = await World.CreateAsync(context);
        context.DepartmentManagers.Add(new DepartmentManager { DepartmentId = world.Other.Id, ManagerId = world.Manager.Id });
        context.DepartmentEmployees.Add(new DepartmentEmployee { DepartmentId = world.Owned.Id, EmployeeId = world.Worker.Id });
        var first = world.Category(world.Owned, "First");
        var second = world.Category(world.Other, "Second");
        world.Entry(world.Task(world.Owned, first, "First"), From, 2m);
        world.Entry(world.Task(world.Other, second, "Second"), From, 3m);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var query = new TopCategoriesQuery(context);

        Assert.Equal(5m, (await query.GetAsync(world.Manager.Id, From, To)).Sum(item => item.TotalHours));
        Assert.Empty(await query.GetAsync(world.Worker.Id, From, To)); // No manager links, never company-wide fallback.
        Assert.Empty(await query.GetAsync(world.Manager.Id, To.AddDays(1), To.AddDays(2)));
        var revoked = await context.DepartmentManagers.SingleAsync(item => item.DepartmentId == world.Other.Id);
        context.DepartmentManagers.Remove(revoked);
        await context.SaveChangesAsync();
        var remaining = await query.GetAsync(world.Manager.Id, From, To);
        Assert.Equal(first.Id.Value, Assert.Single(remaining).CategoryId);
        Assert.Equal(2m, remaining[0].TotalHours);
    }

    private static AppDbContext Context(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);

    private static void Version(TimeEntry entry, WorkTask task, DateOnly date, decimal hours, TimeEntryStatus status) =>
        Assert.True(PersistedTimeEntry.Append(entry, TimeEntryAudit.Create(entry.Id, entry.EmployeeId, task.Id,
            WorkDate.Create(date).Value, Duration.Create(hours).Value, TimeEntryDescription.Create("Historical edit").Value,
            status, entry.CurrentAudit!.AuditTime.AddMinutes(1)).Value).IsSuccess);

    private sealed class World(AppDbContext context, Employee manager, Employee worker, Department owned, Department other)
    {
        public Employee Manager { get; } = manager;
        public Employee Worker { get; } = worker;
        public Department Owned { get; } = owned;
        public Department Other { get; } = other;
        public static async Task<World> CreateAsync(AppDbContext context)
        {
            await context.Database.EnsureCreatedAsync();
            var manager = Employee.Create("Manager", "manager@test.invalid", Created).Value;
            Assert.True(manager.GrantPermission(SystemPermission.DepartmentManager).IsSuccess);
            var worker = Employee.Create("Worker", "worker@test.invalid", Created).Value;
            var owned = Department.Create(DepartmentName.Create("Managed").Value).Value;
            var other = Department.Create(DepartmentName.Create("Other").Value).Value;
            context.Employees.AddRange(manager, worker);
            context.Departments.AddRange(owned, other);
            context.DepartmentManagers.Add(new DepartmentManager { DepartmentId = owned.Id, ManagerId = manager.Id });
            context.DepartmentEmployees.Add(new DepartmentEmployee { DepartmentId = other.Id, EmployeeId = worker.Id });
            await context.SaveChangesAsync();
            return new World(context, manager, worker, owned, other);
        }
        public Catagory Category(Department department, string name)
        {
            var category = department.AddCatagory(Manager.Id, CatagoryName.Create(name).Value, false, Created).Value;
            context.Catagories.Add(category);
            return category;
        }
        public WorkTask Task(Department department, Catagory category, string name)
        {
            var task = department.AddTask(category.Id, TaskName.Create(name).Value, Created).Value;
            context.Tasks.Add(task);
            return task;
        }
        public TimeEntry Entry(WorkTask task, DateOnly date, decimal hours, TimeEntryStatus status = TimeEntryStatus.ReadyForApproval)
        {
            // Read-side fixtures materialize all supported historical states.
            var id = TimeEntryId.Create(Guid.NewGuid()).Value;
            var entry = PersistedTimeEntry.Create(Worker.Id, TimeEntryAudit.Create(id, Worker.Id, task.Id,
                WorkDate.Create(date).Value, Duration.Create(hours).Value, TimeEntryDescription.Create("Private comment").Value,
                status, Created).Value).Value;
            context.TimeEntries.Add(entry);
            return entry;
        }
    }
}
