namespace Application.Handlers;
using Application;
using Application.Commands;
using Domain.Interfaces.IUnitOfWork;
using Core.Tools.OperationResult;

using Domain.Interfaces;
using Domain.ValueObjects;


public sealed class CloseOrderHandler(
    IUserRepository userRepository,
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CloseOrderCommand>
{
    public async Task<Result> HandleAsync(CloseOrderCommand command)
    {
        var actorIdResult = UserId.Create(command.ActorId);

        if (actorIdResult.IsFailure)
            return Result.Failure(actorIdResult.Errors.ToArray());

        var actor = await userRepository.GetAsync(actorIdResult.Value);

        if (actor is null)
        {
            return Result.Failure(
                new Error(
                    "UserNotFound",
                    "The current user could not be found."));
        }

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

        var result = order.Close(
            actor.UserId,
            actor.Role,
            actor.Status);

        if (result.IsFailure)
            return result;


        return Result.Success();
    }
}
