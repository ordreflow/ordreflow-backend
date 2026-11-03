using Application.Handlers;
using Application.Queries;
using Domain.ValueObjects;

namespace UnitTests.Features.FR09;

public class AssignedOrderQueryTests
{
    // An employee's list uses their manager's ID and projects only the supplied scoped orders.
    [Fact]
    public async Task AssignedEmployee_ListsOrdersForOwnManager()
    {
        var manager = AssignedWorkTestData.CreateUser("manager@example.com", UserRole.Manager);
        var otherManager = AssignedWorkTestData.CreateUser("other@example.com", UserRole.Manager);
        var employee = AssignedWorkTestData.CreateAssignedEmployee(manager);
        var first = AssignedWorkTestData.CreateOrder(manager, "Repair");
        var second = AssignedWorkTestData.CreateOrder(manager, "Installation");
        var outside = AssignedWorkTestData.CreateOrder(otherManager, "Outside");
        var orders = new AssignedWorkOrderRepository(first, outside, second);

        var result = await new GetOrdersHandler(new AssignedWorkUserRepository(employee), orders)
            .HandleAsync(new GetOrdersQuery(employee.UserId.Value));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        var repair = Assert.Single(result.Value, item => item.Id == first.Id.Value);
        Assert.Equal("Repair", repair.Name);
        Assert.Equal("Open", repair.Status);
        Assert.Null(repair.ClosedAt);
        Assert.Contains(result.Value, item => item.Id == second.Id.Value);
        Assert.DoesNotContain(result.Value, item => item.Id == outside.Id.Value);
        Assert.Equal(manager.UserId, Assert.Single(orders.RequestedManagerIds));
    }

    // Detail lookup allows an employee's manager's order but forbids another manager's order.
    [Fact]
    public async Task AssignedEmployee_CanViewOwnManagersOrderButNotAnOutsideOrder()
    {
        var manager = AssignedWorkTestData.CreateUser("manager@example.com", UserRole.Manager);
        var otherManager = AssignedWorkTestData.CreateUser("other@example.com", UserRole.Manager);
        var employee = AssignedWorkTestData.CreateAssignedEmployee(manager);
        var own = AssignedWorkTestData.CreateOrder(manager, "Repair");
        var outside = AssignedWorkTestData.CreateOrder(otherManager, "Outside");
        var handler = new GetOrderHandler(new AssignedWorkUserRepository(employee),
            new AssignedWorkOrderRepository(own, outside));

        var visible = await handler.HandleAsync(new GetOrderQuery(employee.UserId.Value, own.Id.Value));
        var forbidden = await handler.HandleAsync(new GetOrderQuery(employee.UserId.Value, outside.Id.Value));

        Assert.True(visible.IsSuccess);
        Assert.Equal(own.Id.Value, visible.Value.Id);
        Assert.Equal("Repair", visible.Value.Name);
        Assert.True(forbidden.IsFailure);
        Assert.Equal("OrderAccessForbidden", Assert.Single(forbidden.Errors).Code);
    }

    // Detail lookup distinguishes invalid or unknown actors and invalid or unknown orders.
    [Theory]
    [InlineData("emptyActor", "UserIdEmpty")]
    [InlineData("missingActor", "UserNotFound")]
    [InlineData("emptyOrder", "CaseIdEmpty")]
    [InlineData("missingOrder", "OrderNotFound")]
    public async Task OrderDetail_InvalidOrMissingIdentifiers_ReturnCurrentError(string scenario, string errorCode)
    {
        var manager = AssignedWorkTestData.CreateUser("manager@example.com", UserRole.Manager);
        var employee = AssignedWorkTestData.CreateAssignedEmployee(manager);
        var order = AssignedWorkTestData.CreateOrder(manager, "Repair");
        var actorId = scenario == "emptyActor" ? Guid.Empty
            : scenario == "missingActor" ? Guid.NewGuid() : employee.UserId.Value;
        var orderId = scenario == "emptyOrder" ? Guid.Empty
            : scenario == "missingOrder" ? Guid.NewGuid() : order.Id.Value;

        var result = await new GetOrderHandler(new AssignedWorkUserRepository(employee),
                new AssignedWorkOrderRepository(order))
            .HandleAsync(new GetOrderQuery(actorId, orderId));

        Assert.True(result.IsFailure);
        Assert.Equal(errorCode, Assert.Single(result.Errors).Code);
    }
}
