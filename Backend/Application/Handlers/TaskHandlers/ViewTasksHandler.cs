using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class ViewTasksHandler(
    IUserRepository userRepository,
    IOrderRepository orderRepository,
    ITaskRepository taskRepository) : IQueryHandler<
        ViewTasksQuery,
        Result<IReadOnlyList<TaskDto>>>
{
    public async Task<Result<IReadOnlyList<TaskDto>>> HandleAsync(
        ViewTasksQuery query)
    {
        var actorIdResult = UserId.Create(query.ActorId);

        if (actorIdResult.IsFailure)
            return Result<IReadOnlyList<TaskDto>>.Failure(
                actorIdResult.Errors.ToArray());

        var actor = await userRepository.GetAsync(actorIdResult.Value);

        if (actor is null)
        {
            return Result<IReadOnlyList<TaskDto>>.Failure(
                new Error("UserNotFound", "The current user could not be found."));
        }

        IReadOnlyList<Order> visibleOrders;

        if (actor.Role == UserRole.Admin)
        {
            visibleOrders = await orderRepository.GetAllAsync();
        }
        else if (actor.Role == UserRole.Manager)
        {
            visibleOrders = await orderRepository.GetByManagerIdAsync(actor.UserId);
        }
        else if (actor.ManagerId is null)
        {
            return Result<IReadOnlyList<TaskDto>>.Success(
                Array.Empty<TaskDto>());
        }
        else
        {
            visibleOrders = await orderRepository.GetByManagerIdAsync(actor.ManagerId);
        }

        if (query.OrderId is { } requestedOrderId)
        {
            var orderIdResult = OrderId.Create(requestedOrderId);

            if (orderIdResult.IsFailure)
                return Result<IReadOnlyList<TaskDto>>.Failure(
                    orderIdResult.Errors.ToArray());

            visibleOrders = visibleOrders
                .Where(order => order.Id == orderIdResult.Value)
                .ToArray();
        }

        var tasks = new List<Domain.Entities.Task>();

        foreach (var order in visibleOrders)
            tasks.AddRange(await taskRepository.GetByOrderIdAsync(order.Id));

        return Result<IReadOnlyList<TaskDto>>.Success(tasks.Select(ToDto).ToArray());
    }

    private static TaskDto ToDto(Domain.Entities.Task task) => new(
        task.TaskId.Value,
        task.OrderId?.Value ?? Guid.Empty,
        task.Title,
        task.Description);
}
