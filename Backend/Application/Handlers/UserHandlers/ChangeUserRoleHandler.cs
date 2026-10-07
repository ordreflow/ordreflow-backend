using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class ChangeUserRoleHandler(
    IUserRepository userRepository) : ICommandHandler<ChangeUserRoleCommand>
{
    public async Task<Result> HandleAsync(ChangeUserRoleCommand command)
    {
        var actorId = UserId.Create(command.ActorId);
        var targetId = UserId.Create(command.UserId);
        if (actorId.IsFailure || targetId.IsFailure)
            return Result.Failure((actorId.IsFailure ? actorId.Errors : targetId.Errors).ToArray());
        if (!Enum.TryParse<UserRole>(command.Role, true, out var role))
            return Result.Failure(new Error("InvalidRole", "The selected role is invalid."));

        var actor = await userRepository.GetAsync(actorId.Value);
        var user = await userRepository.GetAsync(targetId.Value);
        if (actor is null || user is null)
            return Result.Failure(new Error("UserNotFound", "The actor or target user was not found."));

        return user.ChangeRole(actor.Role, actor.Status, role);
    }
}
