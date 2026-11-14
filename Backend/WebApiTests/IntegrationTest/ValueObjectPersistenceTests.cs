using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Persistence.Identity;

namespace WebApiTests.IntegrationTest;

public sealed class ValueObjectPersistenceTests
{
    [Fact]
    public async Task Identity_period_and_assignment_values_round_trip_with_typed_foreign_keys()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();

        var employee = Employee.Create(" Jane Doe ", " Jane@Example.com ", DateTime.UtcNow).Value;
        var department = new Department
        {
            Id = DepartmentId.Create(Guid.NewGuid()).Value,
            Name = DepartmentName.Create(" Operations ").Value
        };
        var category = new Catagory
        {
            Id = CatagoryId.Create(Guid.NewGuid()).Value,
            DepartmentId = department.Id, CreatedBy = employee.Id,
            Name = CatagoryName.Create(" Internal work ").Value,
            Status = CatagoryStatus.Active, CreatedAt = DateTime.UtcNow
        };
        var task = new Domain.Entities.Task
        {
            Id = TaskId.Create(Guid.NewGuid()).Value, CatagoryId = category.Id,
            Name = TaskName.Create(" Planning ").Value,
            Status = Domain.ValueObjects.TaskStatus.Active, CreatedAt = DateTime.UtcNow
        };
        var deadline = new DateOnly(2026, 12, 1);
        context.Employees.Add(employee);
        context.Departments.Add(department);
        context.Catagories.Add(category);
        context.Tasks.Add(task);
        context.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(), EmployeeId = employee.Id,
            UserName = employee.Email.Value, Email = employee.Email.Value
        });
        context.TaskAssinments.Add(new Domain.Entities.TaskAssignment
        {
            TaskId = task.Id, AssignedTo = employee.Id,
            ExpectedDuration = PlannedDuration.Create(40m).Value,
            ExpectedDeadline = TargetCompletionDate.Create(deadline).Value
        });
        context.RegistrationPeriodModes.Add(
            RegistrationPeriodMode.Create(employee.Id, RegistrationInterval.DayInMonth, 18).Value);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.Equal(employee.Id, (await context.Users.SingleAsync()).EmployeeId);
        Assert.Equal("Jane Doe", (await context.Employees.SingleAsync()).Name.Value);
        Assert.Equal("jane@example.com", (await context.Employees.SingleAsync()).Email.Value);
        Assert.Equal("Operations", (await context.Departments.SingleAsync()).Name.Value);
        Assert.Equal("Internal work", (await context.Catagories.SingleAsync()).Name.Value);
        Assert.Equal("Planning", (await context.Tasks.SingleAsync()).Name.Value);
        var assignment = await context.TaskAssinments.SingleAsync();
        Assert.Equal(task.Id, assignment.TaskId);
        Assert.Equal(employee.Id, assignment.AssignedTo);
        Assert.Equal(40m, assignment.ExpectedDuration.Value);
        Assert.Equal(deadline, assignment.ExpectedDeadline.Value);
        Assert.Equal(18, (await context.RegistrationPeriodModes.SingleAsync()).DayInMonth!.Value);
    }

    [Fact]
    public void PostgreSql_model_keeps_guid_keys_decimal_duration_and_date_only_deadlines()
    {
        // Model and SQL generation only; no database connection is opened.
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation").Options);

        var audit = context.Model.FindEntityType(typeof(Domain.Entities.TimeEntryAudit))!;
        var id = audit.FindProperty(nameof(Domain.Entities.TimeEntryAudit.Id))!;
        Assert.Equal(typeof(Guid), id.GetValueConverter()!.ProviderClrType);
        var duration = audit.FindProperty(nameof(Domain.Entities.TimeEntryAudit.Duration))!;
        Assert.Equal(typeof(decimal), duration.GetValueConverter()!.ProviderClrType);
        Assert.Equal("numeric(5,2)", duration.GetColumnType());
        var assignment = context.Model.FindEntityType(typeof(Domain.Entities.TaskAssignment))!;
        Assert.Equal("date", assignment.FindProperty(nameof(Domain.Entities.TaskAssignment.ExpectedDeadline))!.GetColumnType());
        Assert.Contains("CREATE TABLE", context.Database.GenerateCreateScript());

        var employeeId = EmployeeId.Create(Guid.NewGuid()).Value;
        var tasks = new[] { TaskId.Create(Guid.NewGuid()).Value };
        Assert.Contains("WHERE", context.TimeEntryAudits
            .Where(value => tasks.Contains(value.TaskId) && value.ActorId == employeeId).ToQueryString());
    }
}
