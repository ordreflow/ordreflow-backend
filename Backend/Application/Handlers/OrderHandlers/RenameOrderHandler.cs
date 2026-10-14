using Application.Commands;
using Domain.Interfaces.IUnitOfWork;

namespace Application.Handlers;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;


public sealed class RenameOrderHandler(
    IUserRepository userRepository,
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RenameOrderCommand>
{
    public async Task<Result> HandleAsync(RenameOrderCommand command)
    {
        // Find the user performing the action
        var userIdResult = UserId.Create(command.ActorId);

        if (userIdResult.IsFailure)
            return Result.Failure(userIdResult.Errors.ToArray());

        var user = await userRepository.GetAsync(userIdResult.Value);

        if (user is null)
        {
            return Result.Failure(
                new Error(
                    "UserNotFound",
                    "The current user could not be found."));
        }

        // Find the order
        var orderIdResult = OrderId.Create(command.OrderId);

        if (orderIdResult.IsFailure)
            return Result.Failure(orderIdResult.Errors.ToArray());

        var order = await orderRepository.GetAsync(orderIdResult.Value);

        if (order is null)
        {
            return Result.Failure(
                new Error(
                    "OrderNotFound",
                    "The order could not be found."));
        }

        // Create the OrderName value object
        var orderNameResult = OrderName.Create(command.Name);

        if (orderNameResult.IsFailure)
            return Result.Failure(orderNameResult.Errors.ToArray());

        // Let the domain enforce the business rules
        var renameResult = order.Rename(
            user.UserId,
            user.Role,
            user.Status,
            orderNameResult.Value);

        if (renameResult.IsFailure)
            return renameResult;

        return Result.Success();
    }
}