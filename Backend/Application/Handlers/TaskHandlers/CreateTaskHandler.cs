using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class CreateTaskHandler(
    IOrderRepository orderRepository,
    IUserRepository userRepository) : ICommandHandler<CreateTaskCommand>
{
    public async Task<Result> HandleAsync(CreateTaskCommand command)
    {
        var managerId = UserId.Create(command.ManagerId);
        var orderId = OrderId.Create(command.OrderId);
        if (managerId.IsFailure)
            return Result.Failure(managerId.Errors.ToArray());
        if (orderId.IsFailure)
            return Result.Failure(orderId.Errors.ToArray());

        var manager = await userRepository.GetAsync(managerId.Value);
        var order = await orderRepository.GetAsync(orderId.Value);
        if (manager is null || order is null)
            return Result.Failure(new Error("OrderNotFound", "The manager or order was not found."));

        var task = order.AddWorkItem(
            manager.UserId,
            manager.Role,
            manager.Status,
            command.Title,
            command.Description);

        if (task.IsFailure)
            return Result.Failure(task.Errors.ToArray());

        return Result.Success();
    }
}
