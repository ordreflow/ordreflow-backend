using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class AssignManagerHandler(
    IUserRepository userRepository) : ICommandHandler<AssignManagerCommand>
{
    public async Task<Result> HandleAsync(AssignManagerCommand command)
    {
        var adminId = UserId.Create(command.AdminId);
        var employeeId = UserId.Create(command.EmployeeId);
        var managerId = UserId.Create(command.ManagerId);
        if (adminId.IsFailure || employeeId.IsFailure || managerId.IsFailure)
            return Result.Failure(
                (adminId.IsFailure ? adminId.Errors : employeeId.IsFailure ? employeeId.Errors : managerId.Errors).ToArray());

        var admin = await userRepository.GetAsync(adminId.Value);
        var employee = await userRepository.GetAsync(employeeId.Value);
        var manager = await userRepository.GetAsync(managerId.Value);
        if (admin is null || employee is null || manager is null)
            return Result.Failure(new Error("UserNotFound", "The admin, employee, or manager was not found."));

        return employee.AssignManager(
            manager.UserId,
            manager.Role,
            manager.Status,
            admin.Role,
            admin.Status);
    }
}
