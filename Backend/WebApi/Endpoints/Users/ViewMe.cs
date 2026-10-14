using Application;
using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Common;
using WebAPI.Contracts.Users;

namespace WebAPI.endpoints.Users;

/// <summary>
/// Gets the authenticated user's own profile.
/// </summary>
public sealed class ViewMe(
    IQueryDispatcher dispatcher,
    IMapper mapper)
    : ApiEndpoint
        .WithoutRequest
        .AndResponse<IResult>
{
    [HttpGet("users/me")]
    public override async Task<IResult> HandleAsync()
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<GetUserQuery, Result<UserDto>>(
            new GetUserQuery(actorId, actorId));

        if (result.IsFailure)
            return result.Errors.ToErrorResult();

        return TypedResults.Ok(mapper.Map<UserResponse>(result.Value));
    }
}
