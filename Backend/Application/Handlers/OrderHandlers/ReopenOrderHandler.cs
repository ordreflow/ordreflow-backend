using Application.Commands;
using Domain.Interfaces.IUnitOfWork;

namespace Application.Handlers;

using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;


public sealed class ReopenOrderHandler(
    IUserRepository userRepository,
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ReopenOrderCommand>
{
    public async Task<Result> HandleAsync(ReopenOrderCommand command)
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

        var result = order.Reopen(
            actor.UserId,
            actor.Role,
            actor.Status);

        if (result.IsFailure)
            return result;


        return Result.Success();
    }
}
