using Application.Handlers;
using Application.Queries;
using Domain.ValueObjects;

namespace UnitTests.Features.FR09;

public class UnassignedEmployeeListTests
{
    // Without a manager assignment, all three employee lists are empty and skip scoped lookups.
    [Fact]
    public async Task UnassignedEmployee_SeesNoOrdersOrTasks()
    {
        var employee = AssignedWorkTestData.CreateUser("employee@example.com", UserRole.Employee);
        var users = new AssignedWorkUserRepository(employee);
        var orders = new AssignedWorkOrderRepository();
        var tasks = new AssignedWorkTaskRepository();

        var orderList = await new GetOrdersHandler(users, orders)
            .HandleAsync(new GetOrdersQuery(employee.UserId.Value));
        var myTasks = await new GetMyTasksHandler(users, orders, tasks)
            .HandleAsync(new GetMyTasksQuery(employee.UserId.Value));
        var taskList = await new ViewTasksHandler(users, orders, tasks)
            .HandleAsync(new ViewTasksQuery(employee.UserId.Value, null));

        Assert.True(orderList.IsSuccess);
        Assert.Empty(orderList.Value);
        Assert.True(myTasks.IsSuccess);
        Assert.Empty(myTasks.Value);
        Assert.True(taskList.IsSuccess);
        Assert.Empty(taskList.Value);
        Assert.Empty(orders.RequestedManagerIds);
        Assert.Empty(tasks.RequestedOrderIds);
    }
}
