using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class DeactivateUserHandler(
    IUserRepository userRepository) : ICommandHandler<DeactivateUserCommand>
{
    public async Task<Result> HandleAsync(DeactivateUserCommand command)
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

        return target.Deactivate(actor.Role, actor.Status);
    }
}
