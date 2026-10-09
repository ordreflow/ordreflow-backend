using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetUserHandler(
    IUserRepository userRepository) : IQueryHandler<
        GetUserQuery,
        Result<UserDto>>
{
    public async Task<Result<UserDto>> HandleAsync(GetUserQuery query)
    {
        var actorIdResult = UserId.Create(query.ActorId);

        if (actorIdResult.IsFailure)
            return Result<UserDto>.Failure(actorIdResult.Errors.ToArray());

        var actor = await userRepository.GetAsync(actorIdResult.Value);

        if (actor is null)
            return Result<UserDto>.Failure(new Error("UserNotFound", "The current user could not be found."));

        var targetIdResult = UserId.Create(query.TargetUserId);

        if (targetIdResult.IsFailure)
            return Result<UserDto>.Failure(targetIdResult.Errors.ToArray());

        var target = await userRepository.GetAsync(targetIdResult.Value);

        if (target is null)
            return Result<UserDto>.Failure(new Error("UserNotFound", "The user could not be found."));

        if (!target.CanView(actor.UserId, actor.Role, actor.Status))
        {
            return Result<UserDto>.Failure(
                new Error("UserAccessForbidden", "You are not authorized to view this user."));
        }

        return Result<UserDto>.Success(new UserDto(
            target.UserId.Value,
            target.Name.Value,
            target.Email.Value,
            target.Role.ToString(),
            target.Status.ToString(),
            target.CreatedAt));
    }
}
