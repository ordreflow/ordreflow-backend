using System.Reflection;
using Domain.Aggregate;
using Domain.Entities;
using Domain.ValueObjects;

namespace WebApiTests.IntegrationTest;

public sealed class DomainEncapsulationTests
{
    [Fact]
    public void Aggregate_and_child_state_cannot_be_written_through_public_setters()
    {
        Type[] types = [typeof(Department), typeof(Catagory), typeof(Domain.Entities.Task), typeof(Employee),
            typeof(EmployeeSystemAccess), typeof(TimeEntry), typeof(Export)];
        foreach (var type in types)
        {
            Assert.All(type.GetProperties(), property => Assert.False(property.SetMethod?.IsPublic ?? false, $"{type.Name}.{property.Name}"));
            Assert.Empty(type.GetConstructors());
        }
        Assert.Null(typeof(TimeEntry).GetMethod("AddAudit", BindingFlags.Public | BindingFlags.Instance));
        Assert.Null(typeof(TimeEntry).GetMethod("Create", BindingFlags.Public | BindingFlags.Static));
        Assert.Null(typeof(Catagory).GetConstructor(Type.EmptyTypes));
    }

    [Fact]
    public void Employee_permission_rows_are_changed_only_through_the_root()
    {
        var employee = Employee.Create("Worker", "worker@test.invalid", DateTime.UtcNow).Value;
        Assert.True(employee.GrantPermission(SystemPermission.Payroll).IsSuccess);
        Assert.True(employee.CanExportTimeEntries());
        Assert.True(employee.RevokePermission(SystemPermission.Employee).IsSuccess);
        Assert.False(employee.CanExportTimeEntries());
        Assert.True(employee.GrantPermission(SystemPermission.Employee).IsSuccess);
        employee.Deactivate();
        Assert.False(employee.CanExportTimeEntries());
    }
}
