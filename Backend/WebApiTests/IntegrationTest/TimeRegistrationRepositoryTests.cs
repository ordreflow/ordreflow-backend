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
        var department = new Department
        {
            Id = DepartmentId.Create(Guid.NewGuid()).Value, Name = DepartmentName.Create("Internal").Value
        };
        var catagory = Category(department.Id, first.Id, internalOnly: true);
        var assignedTask = WorkItem(catagory.Id);
        var generalTask = WorkItem(catagory.Id);
        context.Employees.AddRange(first, second, outsider);
        context.Departments.Add(department);
        context.Catagories.Add(catagory);
        context.Tasks.AddRange(assignedTask, generalTask);
        context.DepartmentEmployees.Add(new DepartmentEmployee
        {
            EmployeeId = first.Id, DepartmentId = department.Id
        });
        context.TaskAssinments.AddRange(
            Assignment(assignedTask.Id, first.Id), Assignment(assignedTask.Id, second.Id));
        await context.SaveChangesAsync();

        var repository = new TimeRegistrationRepository(context);
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
        var department = new Department
        {
            Id = DepartmentId.Create(Guid.NewGuid()).Value, Name = DepartmentName.Create("Work").Value
        };
        var catagory = Category(department.Id, employee.Id, internalOnly: false);
        var task = WorkItem(catagory.Id);
        context.Employees.Add(employee);
        context.Departments.Add(department);
        context.Catagories.Add(catagory);
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var handler = new CreateTimeEntryHandler(new EmployeeAccessRepository(context),
            new TimeRegistrationRepository(context), TimeProvider.System);
        var result = await handler.HandleAsync(new CreateTimeEntryCommand(
            employee.Id.Value, task.Id.Value, new DateTime(2026, 11, 14), 3.5m, null));
        Assert.True(result.IsSuccess);
        await new UnitOfWork(context).SaveChangesAsync();

        context.ChangeTracker.Clear();
        var saved = await context.TimeEntries.AsNoTracking().Include(entry => entry.Audits).SingleAsync();
        Assert.Equal(employee.Id, saved.EmployeeId);
        Assert.Equal(1, saved.Revision);
        var audit = Assert.Single(saved.Audits);
        Assert.Equal(TimeEntryStatus.Draft, audit.Status);
        Assert.Equal(task.Id, audit.TaskId);
        Assert.Equal(employee.Id, audit.ActorId);
        Assert.Equal(new DateOnly(2026, 11, 14), audit.WorkDate.Value);
        Assert.Equal(3.5m, audit.Duration.Value);
        Assert.Equal(string.Empty, audit.Description.Value);
    }

    private static async Task<AppDbContext> CreateContextAsync(SqliteConnection connection)
    {
        await connection.OpenAsync();
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static Catagory Category(DepartmentId departmentId, EmployeeId creatorId, bool internalOnly) => new()
    {
        Id = CatagoryId.Create(Guid.NewGuid()).Value, CreatedBy = creatorId, DepartmentId = departmentId,
        Name = CatagoryName.Create("Work").Value, Status = CatagoryStatus.Active, InternalOnly = internalOnly,
        CreatedAt = DateTime.UtcNow
    };

    private static Domain.Entities.Task WorkItem(CatagoryId catagoryId) => new()
    {
        Id = TaskId.Create(Guid.NewGuid()).Value, CatagoryId = catagoryId,
        Name = TaskName.Create("Task").Value,
        Status = TaskStatus.Active, CreatedAt = DateTime.UtcNow
    };

    private static TaskAssignment Assignment(TaskId taskId, EmployeeId employeeId) => new()
    {
        TaskId = taskId, AssignedTo = employeeId,
        ExpectedDuration = PlannedDuration.Create(2m).Value,
        ExpectedDeadline = TargetCompletionDate.Create(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))).Value
    };
}
