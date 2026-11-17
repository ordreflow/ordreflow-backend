using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

/// <summary>Sets or clears a user's displayed job title. The title never changes the user's system access.</summary>
public sealed class ChangeJobTitleHandler(
    IUserRepository userRepository) : ICommandHandler<ChangeJobTitleCommand>
{
    public async Task<Result> HandleAsync(ChangeJobTitleCommand command)
    {
        var actorId = UserId.Create(command.ActorId);
        var targetId = UserId.Create(command.UserId);
        if (actorId.IsFailure || targetId.IsFailure)
            return Result.Failure((actorId.IsFailure ? actorId.Errors : targetId.Errors).ToArray());

        var jobTitle = JobTitle.CreateOptional(command.JobTitle);
        if (jobTitle.IsFailure)
            return Result.Failure(jobTitle.Errors.ToArray());

        var actor = await userRepository.GetAsync(actorId.Value);
        if (actor is null)
            return Result.Failure(new Error("UserNotFound", "The current user could not be found."));

        var user = await userRepository.GetAsync(targetId.Value);
        if (user is null)
            return Result.Failure(new Error("UserNotFound", "The user could not be found."));

        return user.ChangeJobTitle(actor.Role, actor.Status, jobTitle.Value);
    }
}
