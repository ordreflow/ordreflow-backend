using Domain.Services;
using Domain.ValueObjects;

namespace UnitTests.Features.FR01;

public class TimeRegistrationOwnershipTests
{
    private static readonly DateTime EntryDate = new(2026, 9, 9);

    // Two employees can be in scope, but one cannot register the other's entry or alter it.
    [Fact]
    public void InScopeEmployee_CannotRegisterAnotherEmployeesEntry_WithoutChangingIt()
    {
        var (manager, owner, order, task) =
            TimeRegistrationTestData.CreateAssignedWork(employeeEmail: "owner@example.com");
        var otherEmployee = TimeRegistrationTestData.CreateAssignedEmployee(manager, "other@example.com");
        var entry = TimeRegistrationTestData.CreateEntry(owner, task.TaskId);
        var service = new TimeRegistrationDomainService();

        Assert.Equal(manager.UserId, owner.ManagerId);
        Assert.Equal(manager.UserId, otherEmployee.ManagerId);
        Assert.True(service.Register(owner, order, entry).IsSuccess);
        Assert.True(service.Register(otherEmployee, order,
            TimeRegistrationTestData.CreateEntry(otherEmployee, task.TaskId)).IsSuccess);

        var result = service.Register(otherEmployee, order, entry);

        Assert.True(result.IsFailure);
        Assert.Equal("EntryOwnerMismatch", Assert.Single(result.Errors).Code);
        Assert.Equal(owner.UserId, entry.EmployeeId);
        Assert.Equal(task.TaskId, entry.TaskId);
        Assert.Equal(EntryDate, entry.Date);
        Assert.Equal(2m, entry.Hours);
        Assert.Equal("Repair", entry.Comment);
        Assert.Equal(TimeEntryStatus.Draft, entry.Status);
        Assert.Empty(entry.Reviews);
    }
}
