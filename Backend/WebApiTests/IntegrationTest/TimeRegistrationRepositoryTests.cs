using Application.Commands;
using Application.Handlers;
using Domain.Aggregate;
using DepartmentEmployee = Domain.Entities.DepartmentEmployee;
using TaskAssignment = Domain.Entities.TaskAssignment;
using Domain.Interfaces;
using Domain.ValueObjects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Repositories;
using TaskStatus = Domain.ValueObjects.TaskStatus;
using Domain.Services;
using Catagory = Domain.Entities.Catagory;

namespace WebApiTests.IntegrationTest;

public sealed class TimeRegistrationRepositoryTests
{
    [Fact]
    public async Task Department_and_assignee_lookups_use_the_employee_and_task_ids()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await using var context = await CreateContextAsync(connection);
        var first = Employee.Create("First", "first@test.invalid", DateTime.UtcNow).Value;
        var second = Employee.Create("Second", "second@test.invalid", DateTime.UtcNow).Value;
        var outsider = Employee.Create("Outsider", "outsider@test.invalid", DateTime.UtcNow).Value;
        var department = Department.Create(DepartmentName.Create("Internal").Value).Value;
        var catagory = Category(department, first.Id, internalOnly: true);
        var assignedTask = WorkItem(department, catagory.Id);
        var generalTask = WorkItem(department, catagory.Id);
        var otherDepartment = Department.Create(DepartmentName.Create("Other").Value).Value;
        var otherCategory = Category(otherDepartment, first.Id, internalOnly: false);
        var otherTask = WorkItem(otherDepartment, otherCategory.Id);
        context.Employees.AddRange(first, second, outsider);
        context.Departments.Add(department);
        context.Catagories.Add(catagory);
        context.Tasks.AddRange(assignedTask, generalTask);
        context.Departments.Add(otherDepartment);
        context.Catagories.Add(otherCategory);
        context.Tasks.Add(otherTask);
        context.DepartmentEmployees.Add(new DepartmentEmployee
        {
            EmployeeId = first.Id, DepartmentId = department.Id
        });
        context.TaskAssinments.AddRange(
            Assignment(assignedTask.Id, first.Id), Assignment(assignedTask.Id, second.Id));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repository = new TimeRegistrationRepository(context);
        var loaded = await repository.GetDepartmentForTaskAsync(assignedTask.Id);
        Assert.NotNull(loaded);
        Assert.Equal(department.Id, loaded.Id);
        Assert.Equal(2, Assert.Single(loaded.Catagories).Tasks.Count);
        Assert.True(loaded.CanRegisterTime(assignedTask.Id).IsSuccess);
        Assert.True(loaded.CanRegisterTime(generalTask.Id).IsSuccess);
        Assert.Equal("TaskNotFound", Assert.Single(loaded.CanRegisterTime(otherTask.Id).Errors).Code);
        Assert.Equal(otherDepartment.Id, (await repository.GetDepartmentForTaskAsync(otherTask.Id))!.Id);
        Assert.Empty(context.ChangeTracker.Entries<Department>());
        Assert.Null(await repository.GetDepartmentForTaskAsync(TaskId.Create(Guid.NewGuid()).Value));
        Assert.True(await repository.IsDepartmentMemberAsync(first.Id, department.Id));
        Assert.False(await repository.IsDepartmentMemberAsync(second.Id, department.Id));
        Assert.False(await repository.IsDepartmentMemberAsync(outsider.Id, department.Id));
        Assert.Equal(new TaskAssignmentAccess(true, true), await repository.GetAssignmentAccessAsync(assignedTask.Id, first.Id));
        Assert.Equal(new TaskAssignmentAccess(true, true), await repository.GetAssignmentAccessAsync(assignedTask.Id, second.Id));
        Assert.Equal(new TaskAssignmentAccess(true, false), await repository.GetAssignmentAccessAsync(assignedTask.Id, outsider.Id));
        Assert.Equal(new TaskAssignmentAccess(false, false), await repository.GetAssignmentAccessAsync(generalTask.Id, outsider.Id));
    }

    [Fact]
    public async Task Registration_and_initial_audit_are_saved_together()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await using var context = await CreateContextAsync(connection);
        var employee = Employee.Create("Worker", "worker@test.invalid", DateTime.UtcNow).Value;
        var department = Department.Create(DepartmentName.Create("Work").Value).Value;
        var catagory = Category(department, employee.Id, internalOnly: false);
        var task = WorkItem(department, catagory.Id);
        context.Employees.Add(employee);
        context.Departments.Add(department);
        context.Catagories.Add(catagory);
        context.Tasks.Add(task);
        context.RegistrationPeriodModes.Add(
            RegistrationPeriodMode.Create(employee.Id, RegistrationInterval.Weekly, null).Value);
        await context.SaveChangesAsync();

        var handler = new CreateTimeEntryHandler(new EmployeeAccessRepository(context),
            new TimeRegistrationRepository(context), new RegistrationPeriodModeRepository(context),
            new RegistrationClock(), TimeZoneInfo.Utc, new TimeRegistrationDomainService());
        var result = await handler.HandleAsync(new CreateTimeEntryCommand(
            employee.Id.Value, task.Id.Value, new DateTime(2026, 11, 14), 3.5m, null));
        Assert.True(result.IsSuccess);
        await new UnitOfWork(context).SaveChangesAsync();

        context.ChangeTracker.Clear();
        var saved = await context.TimeEntries.AsNoTracking().Include(entry => entry.Audits).SingleAsync();
        Assert.Equal(employee.Id, saved.EmployeeId);
        Assert.Equal(1, saved.Revision);
        var audit = Assert.Single(saved.Audits);
        Assert.Equal(TimeEntryStatus.ReadyForApproval, audit.Status);
        Assert.Equal(task.Id, audit.TaskId);
        Assert.Equal(employee.Id, audit.ActorId);
        Assert.Equal(new DateOnly(2026, 11, 14), audit.WorkDate.Value);
        Assert.Equal(3.5m, audit.Duration.Value);
        Assert.Equal(string.Empty, audit.Description.Value);
    }

    private sealed class RegistrationClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            new(2026, 11, 14, 12, 0, 0, TimeSpan.Zero);
    }

    private static async Task<AppDbContext> CreateContextAsync(SqliteConnection connection)
    {
        await connection.OpenAsync();
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static Catagory Category(Department department, EmployeeId creatorId, bool internalOnly) =>
        department.AddCatagory(creatorId, CatagoryName.Create("Work").Value, internalOnly, DateTime.UtcNow).Value;

    private static Domain.Entities.Task WorkItem(Department department, CatagoryId category) =>
        department.AddTask(category, TaskName.Create("Task").Value, DateTime.UtcNow).Value;

    private static TaskAssignment Assignment(TaskId taskId, EmployeeId employeeId) => new()
    {
        TaskId = taskId, AssignedTo = employeeId,
        ExpectedDuration = PlannedDuration.Create(2m).Value,
        ExpectedDeadline = TargetCompletionDate.Create(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))).Value
    };
}
