using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class CreateOrderHandler(
    IOrderRepository orderRepository,
    IUserRepository userRepository) : ICommandHandler<CreateOrderCommand>
{
    public async Task<Result> HandleAsync(CreateOrderCommand command)
    {
        var managerId = UserId.Create(command.ManagerId);
        var name = OrderName.Create(command.Name);
        if (managerId.IsFailure)
            return Result.Failure(managerId.Errors.ToArray());
        if (name.IsFailure)
            return Result.Failure(name.Errors.ToArray());

        var manager = await userRepository.GetAsync(managerId.Value);
        if (manager is null)
            return Result.Failure(new Error("ManagerNotFound", "The manager was not found."));

        var order = Order.Create(
            manager.UserId,
            manager.Role,
            manager.Status,
            name.Value);

        if (order.IsFailure)
            return Result.Failure(order.Errors.ToArray());

        await orderRepository.AddAsync(order.Value);
        return Result.Success();
    }
}
