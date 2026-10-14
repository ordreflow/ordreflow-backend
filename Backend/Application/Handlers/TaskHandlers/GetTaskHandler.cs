using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetTaskHandler(
    IUserRepository userRepository,
    IOrderRepository orderRepository,
    ITaskRepository taskRepository) : IQueryHandler<
        GetTaskQuery,
        Result<TaskDto>>
{
    public async Task<Result<TaskDto>> HandleAsync(GetTaskQuery query)
    {
        var actorIdResult = UserId.Create(query.ActorId);

        if (actorIdResult.IsFailure)
            return Result<TaskDto>.Failure(actorIdResult.Errors.ToArray());

        var actor = await userRepository.GetAsync(actorIdResult.Value);

        if (actor is null)
        {
            return Result<TaskDto>.Failure(
                new Error("UserNotFound", "The current user could not be found."));
        }

        var taskIdResult = TaskId.Create(query.TaskId);

        if (taskIdResult.IsFailure)
            return Result<TaskDto>.Failure(taskIdResult.Errors.ToArray());

        var task = await taskRepository.GetAsync(taskIdResult.Value);

        if (task is null || task.OrderId is null)
        {
            return Result<TaskDto>.Failure(
                new Error("TaskNotFound", "The task could not be found."));
        }

        var order = await orderRepository.GetAsync(task.OrderId);

        if (order is null)
        {
            return Result<TaskDto>.Failure(
                new Error("TaskNotFound", "The task could not be found."));
        }

        if (!order.CanView(actor.UserId, actor.Role, actor.ManagerId))
        {
            return Result<TaskDto>.Failure(
                new Error("TaskAccessForbidden", "You are not authorized to view this task."));
        }

        return Result<TaskDto>.Success(new TaskDto(
            task.TaskId.Value,
            task.OrderId.Value,
            task.Title,
            task.Description));
    }
}
