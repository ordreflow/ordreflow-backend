using Application.Exports;
using Application.Commands;
using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Repositories;
using TimeEntryAudit = Domain.Entities.TimeEntryAudit;

namespace WebApiTests.IntegrationTest;

public sealed class PayrollExportRepositoryTests
{
    private static readonly DateTime Created = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly From = new(2026, 11, 1);
    private static readonly DateOnly To = new(2026, 11, 30);

    [Fact]
    public async Task Saved_batches_keep_original_csv_and_close_only_their_fixed_selection_even_when_overlapping()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        var world = await SeedAsync(context);
        var firstEntry = Entry(world.Worker.Id, world.Task.Id, world.Payroll.Id, From);
        var lastEntry = Entry(world.Worker.Id, world.Task.Id, world.Payroll.Id, To);
        context.TimeEntries.AddRange(firstEntry, lastEntry);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new ExportRepository(context);
        var clock = new ExportClock();
        var service = new PayrollExportService(new EmployeeAccessRepository(context), repository, clock);
        var firstId = Guid.NewGuid();
        var first = await service.CreateAsync(world.Payroll.Id.Value,
            new CreateExportCommand(firstId, From, To, world.Worker.Id.Value));
        Assert.True(first.IsSuccess);
        Assert.Equal(2, first.Value.Entries.Count);
        var saved = (await service.GetAsync(world.Payroll.Id.Value, firstId)).Value;
        var originalFile = saved.CsvContent;
        Assert.All(await context.TimeEntries.AsNoTracking().Include(item => item.Audits).ToArrayAsync(),
            entry => Assert.Equal(TimeEntryStatus.Approved, entry.CurrentStatus));

        // A later approved entry belongs to a second batch, not retroactively to the first.
        var later = Entry(world.Worker.Id, world.Task.Id, world.Payroll.Id, From.AddDays(1));
        context.TimeEntries.Add(later);
        var worker = await context.Employees.SingleAsync(item => item.Id == world.Worker.Id);
        worker.Name = PersonName.Create("Renamed worker").Value;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var secondId = Guid.NewGuid();
        var second = await service.CreateAsync(world.Payroll.Id.Value,
            new CreateExportCommand(secondId, From, To, world.Worker.Id.Value));
        Assert.Equal(3, second.Value.Entries.Count);
        var firstAgain = await service.GetAsync(world.Payroll.Id.Value, firstId);
        Assert.Equal(originalFile, firstAgain.Value.CsvContent);

        context.ChangeTracker.Clear();
        Assert.True((await service.CloseTimeEntriesAsync(world.Payroll.Id.Value, firstId)).IsSuccess);
        context.ChangeTracker.Clear();
        var states = await context.TimeEntries.AsNoTracking().Include(item => item.Audits).ToDictionaryAsync(item => item.Id);
        Assert.Equal(TimeEntryStatus.Closed, states[firstEntry.Id].CurrentStatus);
        Assert.Equal(TimeEntryStatus.Closed, states[lastEntry.Id].CurrentStatus);
        Assert.Equal(TimeEntryStatus.Approved, states[later.Id].CurrentStatus);
        Assert.Null((await service.GetAsync(world.Payroll.Id.Value, secondId)).Value.TimeEntriesClosedAtUtc);

        // The second batch skips the entries closed by the first, closing the remainder.
        Assert.True((await service.CloseTimeEntriesAsync(world.Payroll.Id.Value, secondId)).IsSuccess);
        context.ChangeTracker.Clear();
        Assert.True((await service.CloseTimeEntriesAsync(world.Payroll.Id.Value, firstId)).IsSuccess);
        context.ChangeTracker.Clear();
        var closed = await context.TimeEntries.AsNoTracking().Include(item => item.Audits).ToArrayAsync();
        Assert.All(closed, entry =>
        {
            Assert.Equal(TimeEntryStatus.Closed, entry.CurrentStatus);
            Assert.Equal(3, entry.Audits.Count);
            Assert.Equal(world.Payroll.Id, entry.CurrentAudit!.ClosedBy);
        });
        Assert.Equal(originalFile, (await service.GetAsync(world.Payroll.Id.Value, firstId)).Value.CsvContent);
        Assert.Equal(2, (await service.ListAsync(world.Payroll.Id.Value, 1, 25)).Value.Count);
    }

    [Fact]
    public async Task Selection_uses_current_audit_inclusive_dates_and_employee_filter_including_inactive_owners()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        var world = await SeedAsync(context);
        world.Worker.Status = EmployeeStatus.Deactivated;
        var first = Entry(world.Worker.Id, world.Task.Id, world.Payroll.Id, From);
        var last = Entry(world.Worker.Id, world.Task.Id, world.Payroll.Id, To);
        var other = Entry(world.Payroll.Id, world.Task.Id, world.Payroll.Id, From);
        var noLongerApproved = Entry(world.Worker.Id, world.Task.Id, world.Payroll.Id, From);
        var current = noLongerApproved.CurrentAudit!;
        Assert.True(noLongerApproved.AddAudit(TimeEntryAudit.Create(noLongerApproved.Id, world.Worker.Id,
            world.Task.Id, current.WorkDate, current.Duration, current.Description, TimeEntryStatus.Rejected,
            Created.AddMinutes(2)).Value).IsSuccess);
        context.TimeEntries.AddRange(first, last, other, noLongerApproved,
            Entry(world.Worker.Id, world.Task.Id, world.Payroll.Id, From.AddDays(-1)),
            Entry(world.Worker.Id, world.Task.Id, world.Payroll.Id, To.AddDays(1)));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new ExportRepository(context);

        var all = await repository.SelectApprovedAsync(From, To, null, default);
        var selected = await repository.SelectApprovedAsync(From, To, world.Worker.Id, default);
        var people = await repository.EmployeesAsync("Worker", 1, 25, default);

        Assert.Equal(3, all.Count);
        Assert.Equal(new[] { first.Id.Value, last.Id.Value }.Order(), selected.Select(item => item.Entry.Id.Value).Order());
        Assert.Equal("Deactivated", Assert.Single(people).Status);
    }

    private static TimeEntry Entry(EmployeeId owner, TaskId task, EmployeeId reviewer, DateOnly date)
    {
        var id = TimeEntryId.Create(Guid.NewGuid()).Value;
        var entry = TimeEntry.Create(owner, TimeEntryAudit.Create(id, owner, task, WorkDate.Create(date).Value,
            Duration.Create(7.5m).Value, TimeEntryDescription.Create("Work").Value,
            TimeEntryStatus.ReadyForApproval, Created).Value).Value;
        Assert.True(entry.Approve(reviewer, Created.AddMinutes(1), true).IsSuccess);
        return entry;
    }

    private static async Task<World> SeedAsync(AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();
        var payroll = Employee.Create("Payroll", "payroll@test.invalid", Created).Value;
        Assert.True(payroll.GrantPermission(SystemPermission.Payroll).IsSuccess);
        var worker = Employee.Create("Worker", "worker@test.invalid", Created).Value;
        var department = new Department { Id = DepartmentId.Create(Guid.NewGuid()).Value, Name = DepartmentName.Create("Department").Value };
        var category = new Catagory { Id = CatagoryId.Create(Guid.NewGuid()).Value, DepartmentId = department.Id,
            CreatedBy = payroll.Id, Name = CatagoryName.Create("Category").Value, Status = CatagoryStatus.Active, CreatedAt = Created };
        var task = new Domain.Entities.Task { Id = TaskId.Create(Guid.NewGuid()).Value, CatagoryId = category.Id,
            Name = TaskName.Create("Task").Value, Status = Domain.ValueObjects.TaskStatus.Active, CreatedAt = Created };
        context.Employees.AddRange(payroll, worker);
        context.Departments.Add(department);
        context.Catagories.Add(category);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();
        return new World(payroll, worker, task);
    }

    private sealed record World(Employee Payroll, Employee Worker, Domain.Entities.Task Task);
    private sealed class ExportClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 12, 1, 12, 0, 0, TimeSpan.Zero);
    }
}
