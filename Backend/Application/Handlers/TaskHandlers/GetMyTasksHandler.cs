using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetMyTasksHandler(
    IUserRepository userRepository,
    IOrderRepository orderRepository,
    ITaskRepository taskRepository) : IQueryHandler<
        GetMyTasksQuery,
        Result<IReadOnlyList<TaskDto>>>
{
    public async  Task<Result<IReadOnlyList<TaskDto>>> HandleAsync(
        GetMyTasksQuery query)
    {
        var employeeIdResult = UserId.Create(query.EmployeeId);

        if (employeeIdResult.IsFailure)
            return Result<IReadOnlyList<TaskDto>>.Failure(
                employeeIdResult.Errors.ToArray());

        var employee = await userRepository.GetAsync(employeeIdResult.Value);
        if (employee is null)
            return Result<IReadOnlyList<TaskDto>>.Failure(
                new Error("EmployeeNotFound", "The employee was not found."));

        if (employee.ManagerId is null)
            return Result<IReadOnlyList<TaskDto>>.Success(
                Array.Empty<TaskDto>());

        var orders = await orderRepository.GetByManagerIdAsync(employee.ManagerId);
        var tasks = new List<Domain.Entities.Task>();
        foreach (var order in orders)
            tasks.AddRange(await taskRepository.GetByOrderIdAsync(order.Id));

        return Result<IReadOnlyList<TaskDto>>.Success(tasks.Select(ToDto).ToArray());
    }

    private static TaskDto ToDto(Domain.Entities.Task task) => new(
        task.TaskId.Value,
        task.OrderId?.Value ?? Guid.Empty,
        task.Title,
        task.Description);
}
