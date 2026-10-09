using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetOrderHandler(
    IUserRepository userRepository,
    IOrderRepository orderRepository)
    : IQueryHandler<
        GetOrderQuery,
        Result<OrderDto>>
{
    public async Task<Result<OrderDto>> HandleAsync(GetOrderQuery query)
    {
        var actorIdResult = UserId.Create(query.ActorId);

        if (actorIdResult.IsFailure)
            return Result<OrderDto>.Failure(actorIdResult.Errors.ToArray());

        var actor = await userRepository.GetAsync(actorIdResult.Value);

        if (actor is null)
        {
            return Result<OrderDto>.Failure(
                new Error(
                    "UserNotFound",
                    "The current user could not be found."));
        }

        var orderIdResult = OrderId.Create(query.OrderId);

        if (orderIdResult.IsFailure)
            return Result<OrderDto>.Failure(orderIdResult.Errors.ToArray());

        var order = await orderRepository.GetAsync(orderIdResult.Value);

        if (order is null)
        {
            return Result<OrderDto>.Failure(
                new Error(
                    "OrderNotFound",
                    "The order could not be found."));
        }

        if (!order.CanView(actor.UserId, actor.Role, actor.ManagerId))
        {
            return Result<OrderDto>.Failure(
                new Error(
                    "OrderAccessForbidden",
                    "You are not authorized to view this order."));
        }

        return Result<OrderDto>.Success(new OrderDto(
            order.Id.Value,
            order.Name.Value,
            order.Status.ToString(),
            order.CreatedAt,
            order.ClosedAt));
    }
}
