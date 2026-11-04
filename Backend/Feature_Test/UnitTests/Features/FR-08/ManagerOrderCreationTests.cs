using Application.Commands;
using Application.Handlers;
using Domain.ValueObjects;

namespace UnitTests.Features.FR08;

public class ManagerOrderCreationTests
{
    // Creating an order gives it its manager, initial state, and one repository add.
    [Fact]
    public async Task ActiveManager_CreatesOpenOrderWithTrimmedNameAndAddsItOnce()
    {
        var manager = CreationTestData.CreateUser("manager@example.com", UserRole.Manager);
        var users = new CreationUserRepository(manager);
        var orders = new CreationOrderRepository();

        var result = await new CreateOrderHandler(orders, users)
            .HandleAsync(new CreateOrderCommand(manager.UserId.Value, "  Repair order  "));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        var order = Assert.Single(orders.Added);
        Assert.NotEqual(Guid.Empty, order.Id.Value);
        Assert.Equal(manager.UserId, order.ManagerId);
        Assert.Equal("Repair order", order.Name.Value);
        Assert.Equal(OrderStatus.Open, order.Status);
        Assert.NotEqual(default(DateTime), order.CreatedAt);
        Assert.Null(order.ClosedAt);
        Assert.Empty(order.Tasks);
        Assert.Equal(manager.UserId, Assert.Single(users.RequestedIds));
    }

    // Blank and overlong names fail before looking up a manager or adding an order.
    [Theory]
    [InlineData(false, "CaseNameEmpty")]
    [InlineData(true, "CaseNameTooLong")]
    public async Task InvalidOrderName_IsRejectedWithoutRepositoryCalls(bool tooLong, string errorCode)
    {
        var manager = CreationTestData.CreateUser("manager@example.com", UserRole.Manager);
        var users = new CreationUserRepository(manager);
        var orders = new CreationOrderRepository();
        var name = tooLong ? new string('x', 201) : "  ";

        var result = await new CreateOrderHandler(orders, users)
            .HandleAsync(new CreateOrderCommand(manager.UserId.Value, name));

        Assert.True(result.IsFailure);
        Assert.Equal(errorCode, Assert.Single(result.Errors).Code);
        Assert.Empty(users.RequestedIds);
        Assert.Empty(orders.Added);
    }

    // An empty manager ID is invalid; a valid but unknown ID has no manager to own the order.
    [Theory]
    [InlineData(true, "UserIdEmpty")]
    [InlineData(false, "ManagerNotFound")]
    public async Task InvalidOrMissingManager_DoesNotCreateOrder(bool emptyId, string errorCode)
    {
        var users = new CreationUserRepository();
        var orders = new CreationOrderRepository();
        var managerId = emptyId ? Guid.Empty : Guid.NewGuid();

        var result = await new CreateOrderHandler(orders, users)
            .HandleAsync(new CreateOrderCommand(managerId, "Repair order"));

        Assert.True(result.IsFailure);
        Assert.Equal(errorCode, Assert.Single(result.Errors).Code);
        Assert.Empty(orders.Added);
        Assert.Equal(emptyId ? 0 : 1, users.RequestedIds.Count);
    }

    // An employee or an inactive manager cannot create an order, even with a valid name.
    [Theory]
    [InlineData(UserRole.Employee, false)]
    [InlineData(UserRole.Manager, true)]
    public async Task UnauthorizedActor_CannotCreateOrder(UserRole role, bool inactive)
    {
        var actor = CreationTestData.CreateUser("actor@example.com", role);
        if (inactive)
            Assert.True(actor.Deactivate(UserRole.Admin, UserStatus.Active).IsSuccess);
        var orders = new CreationOrderRepository();

        var result = await new CreateOrderHandler(orders, new CreationUserRepository(actor))
            .HandleAsync(new CreateOrderCommand(actor.UserId.Value, "Repair order"));

        Assert.True(result.IsFailure);
        Assert.Equal("CaseCreationForbidden", Assert.Single(result.Errors).Code);
        Assert.Empty(orders.Added);
    }
}
