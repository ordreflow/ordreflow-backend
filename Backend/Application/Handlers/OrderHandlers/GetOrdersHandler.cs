using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetOrdersHandler(
    IUserRepository userRepository,
    IOrderRepository orderRepository)
    : IQueryHandler<
        GetOrdersQuery,
        Result<IReadOnlyList<OrderDto>>>
{
    public async Task<Result<IReadOnlyList<OrderDto>>> HandleAsync(
        GetOrdersQuery query)
    {
        var actorIdResult = UserId.Create(query.ActorId);

        if (actorIdResult.IsFailure)
        {
            return Result<IReadOnlyList<OrderDto>>.Failure(
                actorIdResult.Errors.ToArray());
        }

        var actor = await userRepository.GetAsync(actorIdResult.Value);

        if (actor is null)
        {
            return Result<IReadOnlyList<OrderDto>>.Failure(
                new Error(
                    "UserNotFound",
                    "The current user could not be found."));
        }

        if (actor.Role == UserRole.Admin)
        {
            var orders = await orderRepository.GetAllAsync();

            return Result<IReadOnlyList<OrderDto>>.Success(orders.Select(ToDto).ToArray());
        }

        if (actor.Role == UserRole.Manager)
        {
            var orders = await orderRepository
                .GetByManagerIdAsync(actor.UserId);

            return Result<IReadOnlyList<OrderDto>>.Success(orders.Select(ToDto).ToArray());
        }

        if (actor.ManagerId is null)
        {
            return Result<IReadOnlyList<OrderDto>>.Success(
                Array.Empty<OrderDto>());
        }

        var managerOrders = await orderRepository
            .GetByManagerIdAsync(actor.ManagerId);

        return Result<IReadOnlyList<OrderDto>>.Success(managerOrders.Select(ToDto).ToArray());
    }

    private static OrderDto ToDto(Order order) => new(
        order.Id.Value,
        order.Name.Value,
        order.Status.ToString(),
        order.CreatedAt,
        order.ClosedAt);
}