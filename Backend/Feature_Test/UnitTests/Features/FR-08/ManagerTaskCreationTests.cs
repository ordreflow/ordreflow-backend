using Application.Commands;
using Application.Handlers;
using Domain.ValueObjects;
using TaskStatus = Domain.ValueObjects.TaskStatus;

namespace UnitTests.Features.FR08;

public class ManagerTaskCreationTests
{
    // Task creation attaches one open task to the loaded order, without adding another order.
    [Fact]
    public async Task OwningManager_CreatesTaskAttachedToOpenOrder()
    {
        var manager = CreationTestData.CreateUser("manager@example.com", UserRole.Manager);
        var order = CreationTestData.CreateOrder(manager);
        var orders = new CreationOrderRepository(order);

        var result = await new CreateTaskHandler(orders, new CreationUserRepository(manager))
            .HandleAsync(new CreateTaskCommand(order.Id.Value, manager.UserId.Value,
                "  Repair  ", "  Replace the part  "));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        var task = Assert.Single(order.Tasks);
        Assert.NotEqual(Guid.Empty, task.TaskId.Value);
        Assert.Equal(order.Id, task.OrderId);
        Assert.True(order.ContainsWorkItem(task.TaskId));
        Assert.Equal("Repair", task.Title);
        Assert.Equal("Replace the part", task.Description);
        Assert.Equal(TaskStatus.Open, task.Status);
        Assert.Null(task.ClosedAt);
        Assert.Equal(order.Id, Assert.Single(orders.RequestedIds));
        Assert.Empty(orders.Added);
    }

    // Invalid IDs stop at validation; missing users and orders both return OrderNotFound here.
    [Theory]
    [InlineData("emptyManager", "UserIdEmpty")]
    [InlineData("emptyOrder", "CaseIdEmpty")]
    [InlineData("missingManager", "OrderNotFound")]
    [InlineData("missingOrder", "OrderNotFound")]
    public async Task InvalidOrMissingReference_DoesNotAttachTask(string scenario, string errorCode)
    {
        var manager = CreationTestData.CreateUser("manager@example.com", UserRole.Manager);
        var order = CreationTestData.CreateOrder(manager);
        var orders = new CreationOrderRepository(order);
        var command = new CreateTaskCommand(
            scenario == "emptyOrder" ? Guid.Empty : scenario == "missingOrder" ? Guid.NewGuid() : order.Id.Value,
            scenario == "emptyManager" ? Guid.Empty : scenario == "missingManager" ? Guid.NewGuid() : manager.UserId.Value,
            "Repair", "Replace the part");

        var result = await new CreateTaskHandler(orders, new CreationUserRepository(manager))
            .HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(errorCode, Assert.Single(result.Errors).Code);
        Assert.Empty(order.Tasks);
        Assert.Empty(orders.Added);
        if (scenario is "emptyManager" or "emptyOrder")
            Assert.Empty(orders.RequestedIds);
    }

    // An employee, another manager, and an inactive owner cannot attach work to this order.
    [Theory]
    [InlineData(UserRole.Employee, false)]
    [InlineData(UserRole.Manager, false)]
    [InlineData(UserRole.Manager, true)]
    public async Task UnauthorizedActor_CannotAttachTask(UserRole role, bool inactiveOwner)
    {
        var manager = CreationTestData.CreateUser("manager@example.com", UserRole.Manager);
        var order = CreationTestData.CreateOrder(manager);
        var actor = inactiveOwner ? manager : CreationTestData.CreateUser("other@example.com", role);
        if (inactiveOwner)
            Assert.True(actor.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);
        var orders = new CreationOrderRepository(order);

        var result = await new CreateTaskHandler(orders, new CreationUserRepository(actor))
            .HandleAsync(new CreateTaskCommand(order.Id.Value, actor.UserId.Value, "Repair", "Replace the part"));

        Assert.True(result.IsFailure);
        Assert.Equal("CaseManagementForbidden", Assert.Single(result.Errors).Code);
        Assert.Empty(order.Tasks);
        Assert.Empty(orders.Added);
    }

    // Invalid task fields or a closed order preserve existing work without adding a task.
    [Theory]
    [InlineData("blankTitle", "WorkTitleRequired")]
    [InlineData("longTitle", "WorkTitleTooLong")]
    [InlineData("nullDescription", "WorkDescriptionRequired")]
    [InlineData("longDescription", "WorkDescriptionTooLong")]
    [InlineData("closedOrder", "CaseClosed")]
    public async Task InvalidTaskOrClosedOrder_DoesNotAttachTask(string scenario, string errorCode)
    {
        var manager = CreationTestData.CreateUser("manager@example.com", UserRole.Manager);
        var order = CreationTestData.CreateOrder(manager);
        var existing = order.AddWorkItem(manager.UserId, manager.Role, manager.Status,
            "Existing task", "Already planned").Value;
        if (scenario == "closedOrder")
            Assert.True(order.Close(manager.UserId, manager.Role, manager.Status).IsSuccess);
        var existingStatus = existing.Status;
        var existingClosedAt = existing.ClosedAt;
        var orders = new CreationOrderRepository(order);
        var title = scenario switch
        {
            "blankTitle" => "  ",
            "longTitle" => new string('x', 201),
            _ => "Repair"
        };
        var description = scenario switch
        {
            "nullDescription" => null!,
            "longDescription" => new string('x', 2001),
            _ => "Replace the part"
        };

        var result = await new CreateTaskHandler(orders, new CreationUserRepository(manager))
            .HandleAsync(new CreateTaskCommand(order.Id.Value, manager.UserId.Value, title, description));

        Assert.True(result.IsFailure);
        Assert.Equal(errorCode, Assert.Single(result.Errors).Code);
        Assert.Same(existing, Assert.Single(order.Tasks));
        Assert.Equal(order.Id, existing.OrderId);
        Assert.Equal("Existing task", existing.Title);
        Assert.Equal("Already planned", existing.Description);
        Assert.Equal(existingStatus, existing.Status);
        Assert.Equal(existingClosedAt, existing.ClosedAt);
        Assert.Empty(orders.Added);
    }
}
