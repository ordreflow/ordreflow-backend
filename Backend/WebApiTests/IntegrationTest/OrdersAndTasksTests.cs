using System.Net;
using System.Net.Http.Json;
using WebApiTests.IntegrationTest.Factories;
using WebAPI.Contracts.Orders;
using WebAPI.Contracts.Tasks;

namespace WebApiTests.IntegrationTest;

// 2. Oprettelse og visning af ordrer og opgaver.
public class OrdersAndTasksTests(TestWebAppFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Manager_creates_order_and_sees_it_in_list_and_by_id()
    {
        var s = await CreateScenarioAsync();

        var list = await s.Manager.Client.GetFromJsonAsync<ViewOrdersResponse>("/api/orders");
        var single = await s.Manager.Client.GetFromJsonAsync<OrderResponse>($"/api/orders/{s.OrderId}");

        Assert.Contains(list!.Items, order => order.Id == s.OrderId);
        Assert.Equal("Open", single!.Status);
        Assert.Null(single.ClosedAt);
    }

    [Fact]
    public async Task Order_with_empty_name_is_rejected()
    {
        var s = await CreateScenarioAsync();

        var response = await s.Manager.Client.PostAsJsonAsync("/api/orders", new CreateOrderRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Manager_can_rename_order()
    {
        var s = await CreateScenarioAsync();

        var response = await s.Manager.Client.PutAsJsonAsync($"/api/orders/{s.OrderId}",
            new UpdateOrderRequest("Renamed order"));

        Assert.True(response.IsSuccessStatusCode);
        var order = await s.Manager.Client.GetFromJsonAsync<OrderResponse>($"/api/orders/{s.OrderId}");
        Assert.Equal("Renamed order", order!.Name);
    }

    [Fact]
    public async Task Employee_sees_orders_of_own_manager_but_other_managers_do_not()
    {
        var s = await CreateScenarioAsync();
        var otherManager = await CreateUserAsync(s.Admin, "Manager");

        var employeeOrders = await s.Employee.Client.GetFromJsonAsync<ViewOrdersResponse>("/api/orders");
        var otherOrders = await otherManager.Client.GetFromJsonAsync<ViewOrdersResponse>("/api/orders");

        Assert.Contains(employeeOrders!.Items, order => order.Id == s.OrderId);
        Assert.DoesNotContain(otherOrders!.Items, order => order.Id == s.OrderId);
    }

    [Fact]
    public async Task Admin_sees_all_orders()
    {
        var s = await CreateScenarioAsync();

        var orders = await s.Admin.Client.GetFromJsonAsync<ViewOrdersResponse>("/api/orders");

        Assert.Contains(orders!.Items, order => order.Id == s.OrderId);
    }

    [Fact]
    public async Task Manager_can_close_and_reopen_order()
    {
        var s = await CreateScenarioAsync();

        var close = await s.Manager.Client.PostAsync($"/api/orders/{s.OrderId}/close", null);
        var closed = await s.Manager.Client.GetFromJsonAsync<OrderResponse>($"/api/orders/{s.OrderId}");
        var reopen = await s.Manager.Client.PostAsync($"/api/orders/{s.OrderId}/reopen", null);
        var reopened = await s.Manager.Client.GetFromJsonAsync<OrderResponse>($"/api/orders/{s.OrderId}");

        Assert.True(close.IsSuccessStatusCode);
        Assert.Equal("Closed", closed!.Status);
        Assert.NotNull(closed.ClosedAt);
        Assert.True(reopen.IsSuccessStatusCode);
        Assert.Equal("Open", reopened!.Status);
    }

    [Fact]
    public async Task Manager_creates_task_and_sees_it_in_list_and_by_id()
    {
        var s = await CreateScenarioAsync();

        var list = await s.Manager.Client.GetFromJsonAsync<ViewTasksResponse>($"/api/tasks?orderId={s.OrderId}");
        var single = await s.Manager.Client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{s.TaskId}");

        Assert.Contains(list!.Items, task => task.Id == s.TaskId);
        Assert.Equal(s.OrderId, single!.OrderId);
        Assert.Equal("Open", single.Status);
    }

    [Fact]
    public async Task Employee_sees_tasks_of_own_manager_orders()
    {
        var s = await CreateScenarioAsync();

        var tasks = await s.Employee.Client.GetFromJsonAsync<ViewTasksResponse>("/api/tasks");
        var mine = await s.Employee.Client.GetAsync("/api/tasks/me");

        Assert.Contains(tasks!.Items, task => task.Id == s.TaskId);
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
    }

    [Fact]
    public async Task Tasks_can_be_filtered_by_order()
    {
        var s = await CreateScenarioAsync();
        var secondOrder = await CreateOrderAsync(s.Manager);
        var secondTask = await CreateTaskAsync(s.Manager, secondOrder);

        var tasks = await s.Manager.Client.GetFromJsonAsync<ViewTasksResponse>($"/api/tasks?orderId={secondOrder}");

        Assert.Single(tasks!.Items);
        Assert.Equal(secondTask, tasks.Items.Single().Id);
    }

    [Fact]
    public async Task Employee_cannot_create_order_or_task()
    {
        var s = await CreateScenarioAsync();

        var task = await s.Employee.Client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest(s.OrderId, "Forbidden", "x"));

        Assert.Equal(HttpStatusCode.Forbidden, task.StatusCode);
    }

    [Fact]
    public async Task Other_manager_cannot_add_task_to_order()
    {
        var s = await CreateScenarioAsync();
        var otherManager = await CreateUserAsync(s.Admin, "Manager");

        var response = await otherManager.Client.PostAsJsonAsync("/api/tasks",
            new CreateTaskRequest(s.OrderId, "Intruder", "x"));

        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Manager_can_edit_close_reopen_and_delete_task()
    {
        var s = await CreateScenarioAsync();

        var edit = await s.Manager.Client.PutAsJsonAsync($"/api/tasks/{s.TaskId}",
            new UpdateTaskRequest("New title", "New description"));
        var close = await s.Manager.Client.PostAsync($"/api/tasks/{s.TaskId}/close", null);
        var closed = await s.Manager.Client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{s.TaskId}");
        var reopen = await s.Manager.Client.PostAsync($"/api/tasks/{s.TaskId}/reopen", null);
        var delete = await s.Manager.Client.DeleteAsync($"/api/tasks/{s.TaskId}");
        var afterDelete = await s.Manager.Client.GetAsync($"/api/tasks/{s.TaskId}");

        Assert.True(edit.IsSuccessStatusCode);
        Assert.True(close.IsSuccessStatusCode);
        Assert.Equal("Closed", closed!.Status);
        Assert.True(reopen.IsSuccessStatusCode);
        Assert.True(delete.IsSuccessStatusCode);
        Assert.False(afterDelete.IsSuccessStatusCode);
    }
}
