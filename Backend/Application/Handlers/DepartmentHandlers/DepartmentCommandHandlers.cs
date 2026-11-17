using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class CreateDepartmentHandler(
    IUserRepository userRepository,
    IDepartmentRepository departmentRepository) : ICommandHandler<CreateDepartmentCommand>
{
    public async Task<Result> HandleAsync(CreateDepartmentCommand command)
    {
        var actorId = UserId.Create(command.ActorId);
        if (actorId.IsFailure)
            return Result.Failure(actorId.Errors.ToArray());

        var name = DepartmentName.Create(command.Name);
        if (name.IsFailure)
            return Result.Failure(name.Errors.ToArray());

        var actor = await userRepository.GetAsync(actorId.Value);
        if (actor is null)
            return Result.Failure(DepartmentErrors.ActorNotFound);

        var department = Department.Create(actor.Role, actor.Status, name.Value);
        if (department.IsFailure)
            return Result.Failure(department.Errors.ToArray());

        if (await departmentRepository.NameExistsAsync(name.Value))
            return Result.Failure(DepartmentErrors.NameTaken);

        await departmentRepository.AddAsync(department.Value);
        return Result.Success();
    }
}

public sealed class RenameDepartmentHandler(
    IUserRepository userRepository,
    IDepartmentRepository departmentRepository) : ICommandHandler<RenameDepartmentCommand>
{
    public async Task<Result> HandleAsync(RenameDepartmentCommand command)
    {
        var actorId = UserId.Create(command.ActorId);
        var departmentId = DepartmentId.Create(command.DepartmentId);
        if (actorId.IsFailure || departmentId.IsFailure)
            return Result.Failure((actorId.IsFailure ? actorId.Errors : departmentId.Errors).ToArray());

        var name = DepartmentName.Create(command.Name);
        if (name.IsFailure)
            return Result.Failure(name.Errors.ToArray());

        var actor = await userRepository.GetAsync(actorId.Value);
        if (actor is null)
            return Result.Failure(DepartmentErrors.ActorNotFound);

        var department = await departmentRepository.GetAsync(departmentId.Value);
        if (department is null)
            return Result.Failure(DepartmentErrors.NotFound);

        // Check before renaming, so a rejected rename never changes the tracked department.
        if (await departmentRepository.NameExistsAsync(name.Value, department.Id))
            return Result.Failure(DepartmentErrors.NameTaken);

        return department.Rename(actor.Role, actor.Status, name.Value);
    }
}

public sealed class SetDepartmentMemberHandler(
    IUserRepository userRepository,
    IDepartmentRepository departmentRepository) : ICommandHandler<SetDepartmentMemberCommand>
{
    public async Task<Result> HandleAsync(SetDepartmentMemberCommand command)
    {
        var actorId = UserId.Create(command.ActorId);
        var departmentId = DepartmentId.Create(command.DepartmentId);
        var userId = UserId.Create(command.UserId);
        if (actorId.IsFailure || departmentId.IsFailure || userId.IsFailure)
            return Result.Combine(actorId, departmentId, userId);

        var actor = await userRepository.GetAsync(actorId.Value);
        if (actor is null)
            return Result.Failure(DepartmentErrors.ActorNotFound);

        var department = await departmentRepository.GetAsync(departmentId.Value);
        if (department is null)
            return Result.Failure(DepartmentErrors.NotFound);

        var user = await userRepository.GetAsync(userId.Value);
        if (user is null)
            return Result.Failure(new Error("UserNotFound", "The user could not be found."));

        return department.SetMember(actor.Role, actor.Status, user.UserId, user.Role, user.Status, command.IsManager);
    }
}

public sealed class RemoveDepartmentMemberHandler(
    IUserRepository userRepository,
    IDepartmentRepository departmentRepository) : ICommandHandler<RemoveDepartmentMemberCommand>
{
    public async Task<Result> HandleAsync(RemoveDepartmentMemberCommand command)
    {
        var actorId = UserId.Create(command.ActorId);
        var departmentId = DepartmentId.Create(command.DepartmentId);
        var userId = UserId.Create(command.UserId);
        if (actorId.IsFailure || departmentId.IsFailure || userId.IsFailure)
            return Result.Combine(actorId, departmentId, userId);

        var actor = await userRepository.GetAsync(actorId.Value);
        if (actor is null)
            return Result.Failure(DepartmentErrors.ActorNotFound);

        var department = await departmentRepository.GetAsync(departmentId.Value);
        if (department is null)
            return Result.Failure(DepartmentErrors.NotFound);

        return department.RemoveMember(actor.Role, actor.Status, userId.Value);
    }
}

internal static class DepartmentErrors
{
    public static readonly Error ActorNotFound = new("UserNotFound", "The current user could not be found.");
    public static readonly Error NotFound = new("DepartmentNotFound", "The department could not be found.");
    public static readonly Error NameTaken = new("DepartmentNameTaken", "A department with this name already exists.");
}
