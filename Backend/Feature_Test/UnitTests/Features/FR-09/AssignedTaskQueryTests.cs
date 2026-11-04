using Application.Handlers;
using Application.Queries;
using Domain.ValueObjects;

namespace UnitTests.Features.FR09;

public class AssignedTaskQueryTests
{
    // My-tasks traverses the employee's manager's orders, not another manager's orders.
    [Fact]
    public async Task AssignedEmployee_MyTasksIncludesTasksFromOwnManagersOrders()
    {
        var manager = AssignedWorkTestData.CreateUser("manager@example.com", UserRole.Manager);
        var otherManager = AssignedWorkTestData.CreateUser("other@example.com", UserRole.Manager);
        var employee = AssignedWorkTestData.CreateAssignedEmployee(manager);
        var firstOrder = AssignedWorkTestData.CreateOrder(manager, "Repair");
        var secondOrder = AssignedWorkTestData.CreateOrder(manager, "Installation");
        var outsideOrder = AssignedWorkTestData.CreateOrder(otherManager, "Outside");
        var firstTask = AssignedWorkTestData.AddTask(firstOrder, manager, "Replace part");
        var secondTask = AssignedWorkTestData.AddTask(secondOrder, manager, "Install part");
        var outsideTask = AssignedWorkTestData.AddTask(outsideOrder, otherManager, "Outside task");
        var orders = new AssignedWorkOrderRepository(firstOrder, outsideOrder, secondOrder);
        var tasks = new AssignedWorkTaskRepository(firstTask, outsideTask, secondTask);

        var result = await new GetMyTasksHandler(new AssignedWorkUserRepository(employee), orders, tasks)
            .HandleAsync(new GetMyTasksQuery(employee.UserId.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, item => item.Id == firstTask.TaskId.Value && item.OrderId == firstOrder.Id.Value);
        Assert.Contains(result.Value, item => item.Id == secondTask.TaskId.Value && item.OrderId == secondOrder.Id.Value);
        Assert.DoesNotContain(result.Value, item => item.Id == outsideTask.TaskId.Value);
        Assert.Equal(manager.UserId, Assert.Single(orders.RequestedManagerIds));
        Assert.DoesNotContain(outsideOrder.Id, tasks.RequestedOrderIds);
    }

    // The optional order filter is applied within visible orders, including a hidden order ID.
    [Fact]
    public async Task AssignedEmployee_ViewTasksFiltersWithinVisibleOrders()
    {
        var manager = AssignedWorkTestData.CreateUser("manager@example.com", UserRole.Manager);
        var otherManager = AssignedWorkTestData.CreateUser("other@example.com", UserRole.Manager);
        var employee = AssignedWorkTestData.CreateAssignedEmployee(manager);
        var firstOrder = AssignedWorkTestData.CreateOrder(manager, "Repair");
        var secondOrder = AssignedWorkTestData.CreateOrder(manager, "Installation");
        var outsideOrder = AssignedWorkTestData.CreateOrder(otherManager, "Outside");
        var firstTask = AssignedWorkTestData.AddTask(firstOrder, manager, "Replace part");
        var secondTask = AssignedWorkTestData.AddTask(secondOrder, manager, "Install part");
        var outsideTask = AssignedWorkTestData.AddTask(outsideOrder, otherManager, "Outside task");
        var orders = new AssignedWorkOrderRepository(firstOrder, outsideOrder, secondOrder);
        var tasks = new AssignedWorkTaskRepository(firstTask, outsideTask, secondTask);
        var handler = new ViewTasksHandler(new AssignedWorkUserRepository(employee), orders, tasks);

        var all = await handler.HandleAsync(new ViewTasksQuery(employee.UserId.Value, null));
        var filtered = await handler.HandleAsync(new ViewTasksQuery(employee.UserId.Value, secondOrder.Id.Value));
        var hidden = await handler.HandleAsync(new ViewTasksQuery(employee.UserId.Value, outsideOrder.Id.Value));
        var invalid = await handler.HandleAsync(new ViewTasksQuery(employee.UserId.Value, Guid.Empty));

        Assert.True(all.IsSuccess);
        Assert.Equal(2, all.Value.Count);
        Assert.DoesNotContain(all.Value, item => item.Id == outsideTask.TaskId.Value);
        Assert.True(filtered.IsSuccess);
        Assert.Equal(secondTask.TaskId.Value, Assert.Single(filtered.Value).Id);
        Assert.True(hidden.IsSuccess);
        Assert.Empty(hidden.Value);
        Assert.True(invalid.IsFailure);
        Assert.Equal("CaseIdEmpty", Assert.Single(invalid.Errors).Code);
        Assert.All(orders.RequestedManagerIds, id => Assert.Equal(manager.UserId, id));
        Assert.DoesNotContain(outsideOrder.Id, tasks.RequestedOrderIds);
    }

    // Task detail uses the owning order's visibility rule for an assigned employee.
    [Fact]
    public async Task AssignedEmployee_CanViewOwnManagersTaskButNotAnOutsideTask()
    {
        var manager = AssignedWorkTestData.CreateUser("manager@example.com", UserRole.Manager);
        var otherManager = AssignedWorkTestData.CreateUser("other@example.com", UserRole.Manager);
        var employee = AssignedWorkTestData.CreateAssignedEmployee(manager);
        var ownOrder = AssignedWorkTestData.CreateOrder(manager, "Repair");
        var outsideOrder = AssignedWorkTestData.CreateOrder(otherManager, "Outside");
        var ownTask = AssignedWorkTestData.AddTask(ownOrder, manager, "Replace part");
        var outsideTask = AssignedWorkTestData.AddTask(outsideOrder, otherManager, "Outside task");
        var handler = new GetTaskHandler(new AssignedWorkUserRepository(employee),
            new AssignedWorkOrderRepository(ownOrder, outsideOrder),
            new AssignedWorkTaskRepository(ownTask, outsideTask));

        var visible = await handler.HandleAsync(new GetTaskQuery(employee.UserId.Value, ownTask.TaskId.Value));
        var forbidden = await handler.HandleAsync(new GetTaskQuery(employee.UserId.Value, outsideTask.TaskId.Value));

        Assert.True(visible.IsSuccess);
        Assert.Equal(ownTask.TaskId.Value, visible.Value.Id);
        Assert.Equal(ownOrder.Id.Value, visible.Value.OrderId);
        Assert.True(forbidden.IsFailure);
        Assert.Equal("TaskAccessForbidden", Assert.Single(forbidden.Errors).Code);
    }

    // Invalid identifiers and missing tasks or owning orders have distinct current result codes.
    [Theory]
    [InlineData("emptyActor", "UserIdEmpty")]
    [InlineData("missingActor", "UserNotFound")]
    [InlineData("emptyTask", "WorkIdEmpty")]
    [InlineData("missingTask", "TaskNotFound")]
    [InlineData("missingOrder", "TaskNotFound")]
    public async Task TaskDetail_InvalidOrMissingReferences_ReturnCurrentError(string scenario, string errorCode)
    {
        var manager = AssignedWorkTestData.CreateUser("manager@example.com", UserRole.Manager);
        var employee = AssignedWorkTestData.CreateAssignedEmployee(manager);
        var order = AssignedWorkTestData.CreateOrder(manager, "Repair");
        var task = AssignedWorkTestData.AddTask(order, manager, "Replace part");
        var actorId = scenario == "emptyActor" ? Guid.Empty
            : scenario == "missingActor" ? Guid.NewGuid() : employee.UserId.Value;
        var taskId = scenario == "emptyTask" ? Guid.Empty
            : scenario == "missingTask" ? Guid.NewGuid() : task.TaskId.Value;
        var orders = scenario == "missingOrder"
            ? new AssignedWorkOrderRepository()
            : new AssignedWorkOrderRepository(order);

        var result = await new GetTaskHandler(new AssignedWorkUserRepository(employee), orders,
                new AssignedWorkTaskRepository(task))
            .HandleAsync(new GetTaskQuery(actorId, taskId));

        Assert.True(result.IsFailure);
        Assert.Equal(errorCode, Assert.Single(result.Errors).Code);
    }
}
