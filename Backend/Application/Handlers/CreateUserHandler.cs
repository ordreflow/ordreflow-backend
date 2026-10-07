using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class CreateUserHandler(
    IUserRepository userRepository) : ICommandHandler<CreateUserCommand>
{
    public async Task<Result> HandleAsync(CreateUserCommand command)
    {
        var actorId = UserId.Create(command.ActorId);
        var name = PersonName.Create(command.Name);
        var email = EmailAddress.Create(command.Email);
        if (actorId.IsFailure)
            return Result.Failure(actorId.Errors.ToArray());
        if (name.IsFailure)
            return Result.Failure(name.Errors.ToArray());
        if (email.IsFailure)
            return Result.Failure(email.Errors.ToArray());
        if (!Enum.TryParse<UserRole>(command.Role, true, out var role))
            return Result.Failure(new Error("InvalidRole", "The selected role is invalid."));

        var actor = await userRepository.GetAsync(actorId.Value);
        if (actor is null)
            return Result.Failure(new Error("UserNotFound", "The creating user was not found."));

        var user = actor.CreateUser(name.Value, email.Value, role);
        if (user.IsFailure)
            return Result.Failure(user.Errors.ToArray());

        await userRepository.AddAsync(user.Value);
        return Result.Success();
    }
}
