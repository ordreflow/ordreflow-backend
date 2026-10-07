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
        Result<IReadOnlyList<Order>>>
{
    public async Task<Result<IReadOnlyList<Order>>> HandleAsync(
        GetOrdersQuery query)
    {
        var actorIdResult = UserId.Create(query.ActorId);

        if (actorIdResult.IsFailure)
        {
            return Result<IReadOnlyList<Order>>.Failure(
                actorIdResult.Errors.ToArray());
        }

        var actor = await userRepository.GetAsync(actorIdResult.Value);

        if (actor is null)
        {
            return Result<IReadOnlyList<Order>>.Failure(
                new Error(
                    "UserNotFound",
                    "The current user could not be found."));
        }

        if (actor.Role == UserRole.Admin)
        {
            var orders = await orderRepository.GetAllAsync();

            return Result<IReadOnlyList<Order>>.Success(orders);
        }

        if (actor.Role == UserRole.Manager)
        {
            var orders = await orderRepository
                .GetByManagerIdAsync(actor.UserId);

            return Result<IReadOnlyList<Order>>.Success(orders);
        }

        if (actor.ManagerId is null)
        {
            return Result<IReadOnlyList<Order>>.Success(
                Array.Empty<Order>());
        }

        var managerOrders = await orderRepository
            .GetByManagerIdAsync(actor.ManagerId);

        return Result<IReadOnlyList<Order>>.Success(managerOrders);
    }
}