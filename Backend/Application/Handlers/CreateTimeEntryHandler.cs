using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Services;
using Domain.Aggregate;
using Domain.ValueObjects;

namespace Application.Handlers;

public class CreateTimeEntryHandler : ICommandHandler<CreateTimeEntryCommand>
{
    private readonly ITimeEntryRepository _timeEntryRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly TimeRegistrationDomainService _registrationService;

    public CreateTimeEntryHandler(
        ITimeEntryRepository timeEntryRepository,
        IUserRepository userRepository,
        ITaskRepository taskRepository,
        IOrderRepository orderRepository,
        TimeRegistrationDomainService registrationService)
    {
        _timeEntryRepository = timeEntryRepository;
        _userRepository = userRepository;
        _taskRepository = taskRepository;
        _orderRepository = orderRepository;
        _registrationService = registrationService;
    }

    public async Task<Result> HandleAsync(CreateTimeEntryCommand command)
    {
      
      var employeeId = UserId.Create(command.EmployeeId);
        var taskId = TaskId.Create(command.TaskId);

        if (employeeId.IsFailure)
            return Result.Failure(employeeId.Errors.ToArray());
        if (taskId.IsFailure)
            return Result.Failure(taskId.Errors.ToArray());

        var employee = await _userRepository.GetAsync(employeeId.Value);
        if (employee is null)
            return Result.Failure(new Error("EmployeeNotFound", "The employee was not found."));

        var task = await _taskRepository.GetAsync(taskId.Value);
        if (task is null || task.OrderId is null)
            return Result.Failure(new Error("TaskNotFound", "The task was not found."));

        var order = await _orderRepository.GetAsync(task.OrderId);
        if (order is null)
            return Result.Failure(new Error("OrderNotFound", "The order was not found."));

        var entryResult = TimeEntry.Create(
            employeeId.Value,
            taskId.Value,
            command.Date,
            command.Hours,
            command.Comment);

        if (entryResult.IsFailure)
            return Result.Failure(entryResult.Errors.ToArray());

        var registrationResult = _registrationService.Register(
            employee,
            order,
            entryResult.Value);

        if (registrationResult.IsFailure)
            return registrationResult;

        await _timeEntryRepository.AddAsync(entryResult.Value);
        return Result.Success();
    }
}