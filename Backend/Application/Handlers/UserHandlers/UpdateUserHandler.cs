using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class UpdateUserHandler(
    IUserRepository userRepository) : ICommandHandler<UpdateUserCommand>
{
    public async Task<Result> HandleAsync(UpdateUserCommand command)
    {
        var actorIdResult = UserId.Create(command.ActorId);

        if (actorIdResult.IsFailure)
            return Result.Failure(actorIdResult.Errors.ToArray());

        var actor = await userRepository.GetAsync(actorIdResult.Value);

        if (actor is null)
            return Result.Failure(new Error("UserNotFound", "The current user could not be found."));

        var targetIdResult = UserId.Create(command.TargetUserId);

        if (targetIdResult.IsFailure)
            return Result.Failure(targetIdResult.Errors.ToArray());

        var target = await userRepository.GetAsync(targetIdResult.Value);

        if (target is null)
            return Result.Failure(new Error("UserNotFound", "The user could not be found."));

        var nameResult = PersonName.Create(command.Name);

        if (nameResult.IsFailure)
            return Result.Failure(nameResult.Errors.ToArray());

        var emailResult = EmailAddress.Create(command.Email);

        if (emailResult.IsFailure)
            return Result.Failure(emailResult.Errors.ToArray());

        var changeNameResult = target.ChangeName(
            actor.UserId,
            actor.Role,
            actor.Status,
            nameResult.Value);

        if (changeNameResult.IsFailure)
            return changeNameResult;

        return target.ChangeEmail(
            actor.UserId,
            actor.Role,
            actor.Status,
            emailResult.Value);
    }
}
