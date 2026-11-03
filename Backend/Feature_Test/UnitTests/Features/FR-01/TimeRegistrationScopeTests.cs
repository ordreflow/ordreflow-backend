using Core.Tools.OperationResult;
using Domain.Services;
using Domain.ValueObjects;

namespace UnitTests.Features.FR01;

public class TimeRegistrationScopeTests
{
    // An active employee assigned to the order's manager can register on its open task.
    [Fact]
    public void AssignedActiveEmployee_CanRegisterOnOpenTaskInManagersOrder()
    {
        var (manager, employee, order, task) = TimeRegistrationTestData.CreateAssignedWork();
        var entry = TimeRegistrationTestData.CreateEntry(employee, task.TaskId);

        Assert.Equal(UserStatus.Active, employee.Status);
        Assert.Equal(manager.UserId, employee.ManagerId);
        Assert.Equal(order.Id, task.OrderId);

        var result = new TimeRegistrationDomainService().Register(employee, order, entry);

        Assert.True(result.IsSuccess);
        Assert.Equal(TimeEntryStatus.Draft, entry.Status);
        Assert.Empty(entry.Reviews);
    }

    // An employee without an assigned manager cannot register on that manager's task.
    [Fact]
    public void EmployeeWithoutManager_CannotRegisterOnOpenTask()
    {
        var (_, employee, order, task) = TimeRegistrationTestData.CreateWork();
        var entry = TimeRegistrationTestData.CreateEntry(employee, task.TaskId);

        Assert.Null(employee.ManagerId);

        AssertError("ManagerScopeForbidden", new TimeRegistrationDomainService().Register(employee, order, entry));
        Assert.Equal(employee.UserId, entry.EmployeeId);
        Assert.Equal(task.TaskId, entry.TaskId);
        Assert.Equal(TimeEntryStatus.Draft, entry.Status);
        Assert.Empty(entry.Reviews);
    }

    // A task must belong to the selected order, even if another order has the same manager.
    [Fact]
    public void TaskFromDifferentOpenOrderUnderSameManager_CannotBeRegisteredAgainstSelectedOrder()
    {
        var (manager, employee, selectedOrder, selectedTask) =
            TimeRegistrationTestData.CreateAssignedWork(taskTitle: "Selected order task");
        var otherOrder = TimeRegistrationTestData.CreateOrder(manager);
        var otherTask = TimeRegistrationTestData.AddTask(otherOrder, manager, "Other order task");
        var entry = TimeRegistrationTestData.CreateEntry(employee, otherTask.TaskId);
        var service = new TimeRegistrationDomainService();

        Assert.Equal(OrderStatus.Open, selectedOrder.Status);
        Assert.Equal(OrderStatus.Open, otherOrder.Status);
        Assert.True(selectedOrder.ContainsWorkItem(selectedTask.TaskId));
        Assert.False(selectedOrder.ContainsWorkItem(otherTask.TaskId));
        Assert.Equal(otherOrder.Id, otherTask.OrderId);

        AssertError("WorkItemNotFound", service.Register(employee, selectedOrder, entry));
        Assert.Equal(employee.UserId, entry.EmployeeId);
        Assert.Equal(otherTask.TaskId, entry.TaskId);
        Assert.Equal(TimeEntryStatus.Draft, entry.Status);
        Assert.Empty(entry.Reviews);

        Assert.True(service.Register(employee, otherOrder, entry).IsSuccess);
    }

    private static void AssertError(string code, Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, Assert.Single(result.Errors).Code);
    }
}
