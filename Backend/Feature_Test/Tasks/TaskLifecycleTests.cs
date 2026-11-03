using Domain.Aggregate;
using Domain.ValueObjects;
using TaskStatus = Domain.ValueObjects.TaskStatus;

namespace UnitTests.Tasks;

public class TaskLifecycleTests
{
    [Fact]
    public void CloseAndReopenTask_EnforceTransitionsAndTimestamps()
    {
        var manager = CreateUser("manager@example.com", UserRole.Manager);
        var order = CreateOrder(manager);
        var task = AddTask(order, manager, "Repair");

        Assert.Equal(TaskStatus.Open, task.Status);
        Assert.Null(task.ClosedAt);

        Assert.True(order.CloseWorkItem(manager.UserId, manager.Role, manager.Status, task.TaskId).IsSuccess);
        var closedAt = task.ClosedAt;
        Assert.Equal(TaskStatus.Closed, task.Status);
        Assert.NotNull(closedAt);

        AssertError("TaskAlreadyClosed", order.CloseWorkItem(manager.UserId, manager.Role, manager.Status, task.TaskId));
        Assert.Equal(closedAt, task.ClosedAt);

        Assert.True(order.ReopenWorkItem(manager.UserId, manager.Role, manager.Status, task.TaskId).IsSuccess);
        Assert.Equal(TaskStatus.Open, task.Status);
        Assert.Null(task.ClosedAt);

        AssertError("TaskAlreadyOpen", order.ReopenWorkItem(manager.UserId, manager.Role, manager.Status, task.TaskId));
        Assert.Null(task.ClosedAt);
    }

    [Fact]
    public void CloseOrder_ClosesOpenTasksButPreservesEarlierTaskClosure()
    {
        var manager = CreateUser("manager@example.com", UserRole.Manager);
        var order = CreateOrder(manager);
        var previouslyClosed = AddTask(order, manager, "Previously closed");
        var open = AddTask(order, manager, "Still open");
        Assert.True(order.CloseWorkItem(manager.UserId, manager.Role, manager.Status, previouslyClosed.TaskId).IsSuccess);
        var originalClosedAt = previouslyClosed.ClosedAt;

        Assert.True(order.Close(manager.UserId, manager.Role, manager.Status).IsSuccess);

        Assert.Equal(OrderStatus.Closed, order.Status);
        Assert.NotNull(order.ClosedAt);
        Assert.Equal(TaskStatus.Closed, previouslyClosed.Status);
        Assert.Equal(originalClosedAt, previouslyClosed.ClosedAt);
        Assert.Equal(TaskStatus.Closed, open.Status);
        Assert.Equal(order.ClosedAt, open.ClosedAt);

        AssertError("CaseAlreadyClosed", order.Close(manager.UserId, manager.Role, manager.Status));
        Assert.Equal(originalClosedAt, previouslyClosed.ClosedAt);
        Assert.Equal(order.ClosedAt, open.ClosedAt);
    }

    [Fact]
    public void ReopenOrder_DoesNotReopenTasks_AndTaskCanReopenOnlyAfterOrder()
    {
        var manager = CreateUser("manager@example.com", UserRole.Manager);
        var order = CreateOrder(manager);
        var task = AddTask(order, manager, "Repair");
        Assert.True(order.Close(manager.UserId, manager.Role, manager.Status).IsSuccess);
        var taskClosedAt = task.ClosedAt;

        AssertError("CaseClosed", order.ReopenWorkItem(manager.UserId, manager.Role, manager.Status, task.TaskId));
        Assert.Equal(taskClosedAt, task.ClosedAt);

        Assert.True(order.Reopen(manager.UserId, manager.Role, manager.Status).IsSuccess);
        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.Null(order.ClosedAt);
        Assert.Equal(TaskStatus.Closed, task.Status);
        Assert.Equal(taskClosedAt, task.ClosedAt);

        Assert.True(order.ReopenWorkItem(manager.UserId, manager.Role, manager.Status, task.TaskId).IsSuccess);
        Assert.Equal(TaskStatus.Open, task.Status);
        Assert.Null(task.ClosedAt);
    }

    [Fact]
    public void TaskTransitions_RejectTaskFromDifferentOrder_WithoutChangingIt()
    {
        var manager = CreateUser("manager@example.com", UserRole.Manager);
        var order = CreateOrder(manager);
        var otherOrder = CreateOrder(manager);
        var otherTask = AddTask(otherOrder, manager, "Another order's task");

        AssertError("WorkItemNotFound", order.CloseWorkItem(manager.UserId, manager.Role, manager.Status, otherTask.TaskId));
        Assert.Equal(TaskStatus.Open, otherTask.Status);
        Assert.Null(otherTask.ClosedAt);

        Assert.True(otherOrder.CloseWorkItem(manager.UserId, manager.Role, manager.Status, otherTask.TaskId).IsSuccess);
        var closedAt = otherTask.ClosedAt;
        AssertError("WorkItemNotFound", order.ReopenWorkItem(manager.UserId, manager.Role, manager.Status, otherTask.TaskId));
        Assert.Equal(TaskStatus.Closed, otherTask.Status);
        Assert.Equal(closedAt, otherTask.ClosedAt);
    }

    [Theory]
    [InlineData(UserRole.Employee, false)]
    [InlineData(UserRole.Manager, false)] // another manager's order
    [InlineData(UserRole.Manager, true)]  // the order manager, now inactive
    public void TaskTransitions_RejectUnauthorizedActors(UserRole role, bool inactiveOwner)
    {
        var manager = CreateUser("manager@example.com", UserRole.Manager);
        var admin = CreateUser("admin@example.com", UserRole.Admin);
        var actor = inactiveOwner ? manager : CreateUser("other@example.com", role);
        var order = CreateOrder(manager);
        var task = AddTask(order, manager, "Repair");
        if (inactiveOwner)
            Assert.True(actor.Deactivate(admin.Role, admin.Status).IsSuccess);

        AssertError("CaseManagementForbidden", order.CloseWorkItem(actor.UserId, actor.Role, actor.Status, task.TaskId));
        Assert.Equal(TaskStatus.Open, task.Status);
        Assert.Null(task.ClosedAt);

        Assert.True(order.CloseWorkItem(admin.UserId, admin.Role, admin.Status, task.TaskId).IsSuccess);
        var closedAt = task.ClosedAt;
        AssertError("CaseManagementForbidden", order.ReopenWorkItem(actor.UserId, actor.Role, actor.Status, task.TaskId));
        Assert.Equal(TaskStatus.Closed, task.Status);
        Assert.Equal(closedAt, task.ClosedAt);

        Assert.True(order.ReopenWorkItem(admin.UserId, admin.Role, admin.Status, task.TaskId).IsSuccess);
        Assert.Equal(TaskStatus.Open, task.Status);
        Assert.Null(task.ClosedAt);
    }

    private static Order CreateOrder(User manager) => Order.Create(
        manager.UserId, manager.Role, manager.Status, OrderName.Create("Repair order").Value).Value;

    private static Domain.Entities.Task AddTask(Order order, User manager, string title) =>
        order.AddWorkItem(manager.UserId, manager.Role, manager.Status, title, "Repair description").Value;

    private static User CreateUser(string email, UserRole role) => User.Create(
        PersonName.Create("Test User").Value, EmailAddress.Create(email).Value, role).Value;

    private static void AssertError(string code, Core.Tools.OperationResult.Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(code, Assert.Single(result.Errors).Code);
    }
}
