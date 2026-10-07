using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Handlers;

public sealed class GetMyTasksHandler(
    IUserRepository userRepository,
    IOrderRepository orderRepository,
    ITaskRepository taskRepository) : IQueryHandler<
        GetMyTasksQuery,
        Result<IReadOnlyList<Domain.Entities.Task>>>
{
    public async Task<Result<IReadOnlyList<Domain.Entities.Task>>> HandleAsync(
        GetMyTasksQuery query)
    {
        var employee = await userRepository.GetAsync(query.EmployeeId);
        if (employee is null)
            return Result<IReadOnlyList<Domain.Entities.Task>>.Failure(
                new Error("EmployeeNotFound", "The employee was not found."));

        if (employee.ManagerId is null)
            return Result<IReadOnlyList<Domain.Entities.Task>>.Success(
                Array.Empty<Domain.Entities.Task>());

        var orders = await orderRepository.GetByManagerIdAsync(employee.ManagerId);
        var tasks = new List<Domain.Entities.Task>();
        foreach (var order in orders)
            tasks.AddRange(await taskRepository.GetByOrderIdAsync(order.Id));

        return Result<IReadOnlyList<Domain.Entities.Task>>.Success(tasks);
    }
}
