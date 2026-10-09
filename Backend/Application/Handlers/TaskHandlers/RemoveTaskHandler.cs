using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.Services;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class RemoveTaskHandler(
    IUserRepository userRepository,
    IOrderRepository orderRepository,
    ITaskRepository taskRepository,
    ITimeEntryRepository timeEntryRepository,
    TaskRemovalDomainService taskRemovalService) : ICommandHandler<RemoveTaskCommand>
{
    public async Task<Result> HandleAsync(RemoveTaskCommand command)
    {
        var actorIdResult = UserId.Create(command.ActorId);

        if (actorIdResult.IsFailure)
            return Result.Failure(actorIdResult.Errors.ToArray());

        var actor = await userRepository.GetAsync(actorIdResult.Value);

        if (actor is null)
            return Result.Failure(new Error("UserNotFound", "The current user could not be found."));

        var taskIdResult = TaskId.Create(command.TaskId);

        if (taskIdResult.IsFailure)
            return Result.Failure(taskIdResult.Errors.ToArray());

        var task = await taskRepository.GetAsync(taskIdResult.Value);

        if (task is null || task.OrderId is null)
            return Result.Failure(new Error("TaskNotFound", "The task could not be found."));

        var order = await orderRepository.GetAsync(task.OrderId);

        if (order is null)
            return Result.Failure(new Error("TaskNotFound", "The task could not be found."));

        var hasTimeEntries = await timeEntryRepository.ExistsForTaskAsync(taskIdResult.Value);

        return taskRemovalService.RemoveTask(
            order,
            actor.UserId,
            actor.Role,
            actor.Status,
            taskIdResult.Value,
            hasTimeEntries);
    }
}
