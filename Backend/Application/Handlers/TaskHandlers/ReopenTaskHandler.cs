using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class ReopenTaskHandler(
    IUserRepository userRepository,
    IOrderRepository orderRepository,
    ITaskRepository taskRepository) : ICommandHandler<ReopenTaskCommand>
{
    public async Task<Result> HandleAsync(ReopenTaskCommand command)
    {
        var actorId = UserId.Create(command.ActorId);
        var taskId = TaskId.Create(command.TaskId);
        if (actorId.IsFailure)
            return Result.Failure(actorId.Errors.ToArray());
        if (taskId.IsFailure)
            return Result.Failure(taskId.Errors.ToArray());

        var actor = await userRepository.GetAsync(actorId.Value);
        if (actor is null)
            return Result.Failure(new Error("UserNotFound", "The current user could not be found."));

        var task = await taskRepository.GetAsync(taskId.Value);
        if (task?.OrderId is null)
            return Result.Failure(new Error("TaskNotFound", "The task could not be found."));

        var order = await orderRepository.GetAsync(task.OrderId);
        if (order is null)
            return Result.Failure(new Error("TaskNotFound", "The task could not be found."));

        return order.ReopenWorkItem(actor.UserId, actor.Role, actor.Status, taskId.Value);
    }
}
