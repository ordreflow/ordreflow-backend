using Domain.Aggregate;
using Domain.Services;
using Domain.ValueObjects;

namespace UnitTests.Features.FR01;

public class ClosedTaskTimeRegistrationTests
{
    // Closed work rejects new entries without changing an earlier finalized entry;
    // registration succeeds again after the order and task are reopened.
    [Fact]
    public void TimeRegistration_RejectsClosedTask_WithoutChangingEarlierFinalizedEntry()
    {
        var admin = TimeRegistrationTestData.CreateUser("admin@example.com", UserRole.Admin);
        var (manager, employee, order, task) = TimeRegistrationTestData.CreateWork();
        Assert.True(employee.AssignManager(manager.UserId, manager.Role, manager.Status,
            admin.Role, admin.Status).IsSuccess);
        var existingEntry = TimeRegistrationTestData.CreateEntry(employee, task.TaskId);
        var service = new TimeRegistrationDomainService();

        Assert.True(service.Register(employee, order, existingEntry).IsSuccess);
        Assert.True(existingEntry.Accept(manager.UserId, employee.UserId, employee.ManagerId,
            manager.Role, manager.Status).IsSuccess);
        Assert.True(existingEntry.Finalize(manager.UserId, employee.UserId, employee.ManagerId,
            manager.Role, manager.Status).IsSuccess);
        var existingReviews = existingEntry.Reviews.ToArray();
        Assert.True(order.CloseWorkItem(manager.UserId, manager.Role, manager.Status, task.TaskId).IsSuccess);

        var closedTaskAttempt = TimeRegistrationTestData.CreateEntry(employee, task.TaskId);
        AssertRejectedWithoutChangingEntry("TaskClosed", service, employee, order, closedTaskAttempt);
        Assert.Equal(TimeEntryStatus.Finalized, existingEntry.Status);
        Assert.Equal(existingReviews, existingEntry.Reviews);
        Assert.Equal(task.TaskId, existingEntry.TaskId);

        Assert.True(order.Close(manager.UserId, manager.Role, manager.Status).IsSuccess);
        var closedOrderAttempt = TimeRegistrationTestData.CreateEntry(employee, task.TaskId);
        AssertRejectedWithoutChangingEntry("CaseClosed", service, employee, order, closedOrderAttempt);
        Assert.True(order.Reopen(manager.UserId, manager.Role, manager.Status).IsSuccess);
        var stillClosedTaskAttempt = TimeRegistrationTestData.CreateEntry(employee, task.TaskId);
        AssertRejectedWithoutChangingEntry("TaskClosed", service, employee, order, stillClosedTaskAttempt);

        Assert.True(order.ReopenWorkItem(manager.UserId, manager.Role, manager.Status, task.TaskId).IsSuccess);
        Assert.True(service.Register(employee, order,
            TimeRegistrationTestData.CreateEntry(employee, task.TaskId)).IsSuccess);
        Assert.Equal(TimeEntryStatus.Finalized, existingEntry.Status);
        Assert.Equal(existingReviews, existingEntry.Reviews);
    }

    private static void AssertRejectedWithoutChangingEntry(string code, TimeRegistrationDomainService service,
        User employee, Order order, TimeEntry entry)
    {
        var before = (entry.Id, entry.EmployeeId, entry.TaskId, entry.Date, entry.Hours, entry.Comment, entry.Status);

        AssertError(code, service.Register(employee, order, entry));

        Assert.Equal(before.Id, entry.Id);
        Assert.Equal(before.EmployeeId, entry.EmployeeId);
        Assert.Equal(before.TaskId, entry.TaskId);
        Assert.Equal(before.Date, entry.Date);
        Assert.Equal(before.Hours, entry.Hours);
        Assert.Equal(before.Comment, entry.Comment);
        Assert.Equal(before.Status, entry.Status);
        Assert.Empty(entry.Reviews);
    }

    private static void AssertError(string code, Core.Tools.OperationResult.Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, Assert.Single(result.Errors).Code);
    }
}
