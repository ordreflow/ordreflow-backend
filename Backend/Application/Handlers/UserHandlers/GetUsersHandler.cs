using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetUsersHandler(
    IUserRepository userRepository) : IQueryHandler<
        GetUsersQuery,
        Result<IReadOnlyList<UserDto>>>
{
    public async Task<Result<IReadOnlyList<UserDto>>> HandleAsync(GetUsersQuery query)
    {
        var actorIdResult = UserId.Create(query.ActorId);

        if (actorIdResult.IsFailure)
            return Result<IReadOnlyList<UserDto>>.Failure(actorIdResult.Errors.ToArray());

        var actor = await userRepository.GetAsync(actorIdResult.Value);

        if (actor is null)
            return Result<IReadOnlyList<UserDto>>.Failure(
                new Error("UserNotFound", "The current user could not be found."));

        if (actor.Role == UserRole.Admin)
        {
            var all = await userRepository.GetAllAsync();
            return Result<IReadOnlyList<UserDto>>.Success(all.Select(ToDto).ToArray());
        }

        if (actor.Role == UserRole.Manager)
        {
            var employees = await userRepository.GetByManagerIdAsync(actor.UserId);
            var withSelf = employees.Append(actor).Select(ToDto).ToArray();
            return Result<IReadOnlyList<UserDto>>.Success(withSelf);
        }

        return Result<IReadOnlyList<UserDto>>.Success(new[] { ToDto(actor) });
    }

    private static UserDto ToDto(User user) => new(
        user.UserId.Value,
        user.Name.Value,
        user.Email.Value,
        user.Role.ToString(),
        user.Status.ToString(),
        user.CreatedAt);
}
