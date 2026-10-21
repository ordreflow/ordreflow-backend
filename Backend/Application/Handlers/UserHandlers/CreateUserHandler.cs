using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class CreateUserHandler(
    IUserRepository userRepository,
    IUserAccountProvisioner accountProvisioner) : ICommandHandler<CreateUserCommand>
{
    public async Task<Result> HandleAsync(CreateUserCommand command)
    {
        var actorId = UserId.Create(command.ActorId);
        var name = PersonName.Create(command.Name);
        var email = EmailAddress.Create(command.Email);
        UserId? managerId = null;
        if (actorId.IsFailure)
            return Result.Failure(actorId.Errors.ToArray());
        if (name.IsFailure)
            return Result.Failure(name.Errors.ToArray());
        if (email.IsFailure)
            return Result.Failure(email.Errors.ToArray());
        if (command.ManagerId is Guid managerGuid)
        {
            var managerIdResult = UserId.Create(managerGuid);
            if (managerIdResult.IsFailure)
                return Result.Failure(managerIdResult.Errors.ToArray());
            managerId = managerIdResult.Value;
        }
        if (string.IsNullOrWhiteSpace(command.Password))
            return Result.Failure(new Error("PasswordRequired", "A password is required."));
        if (!Enum.TryParse<UserRole>(command.Role, true, out var role))
            return Result.Failure(new Error("InvalidRole", "The selected role is invalid."));

        var actor = await userRepository.GetAsync(actorId.Value);
        if (actor is null)
            return Result.Failure(new Error("UserNotFound", "The creating user was not found."));

        var user = actor.CreateUser(name.Value, email.Value, role);
        if (user.IsFailure)
            return Result.Failure(user.Errors.ToArray());

        if (managerId is not null)
        {
            var manager = await userRepository.GetAsync(managerId);
            if (manager is null)
                return Result.Failure(new Error("ManagerNotFound", "The selected manager was not found."));

            var assignment = user.Value.AssignManager(
                manager.UserId,
                manager.Role,
                manager.Status,
                actor.Role,
                actor.Status);
            if (assignment.IsFailure)
                return assignment;
        }

        // The domain user must be tracked before provisioning, so UserManager's internal
        // SaveChanges can satisfy the AspNetUsers -> users foreign key in one flush.
        await userRepository.AddAsync(user.Value);

        var account = await accountProvisioner.CreateAsync(user.Value.UserId, email.Value, command.Password, role);
        if (account.IsFailure)
            return account;

        return Result.Success();
    }
}
