using Application;
using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Common;
using WebAPI.Contracts.Users;

namespace WebAPI.endpoints.Users;

/// <summary>
/// Gets one user profile by ID.
/// </summary>
public sealed class ViewUser(
    IQueryDispatcher dispatcher,
    IMapper mapper)
    : ApiEndpoint
        .WithRequest<Guid>
        .AndResponse<IResult>
{
    [HttpGet("users/{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public override async Task<IResult> HandleAsync([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<GetUserQuery, Result<UserDto>>(
            new GetUserQuery(actorId, id));

        if (result.IsFailure)
            return result.Errors.ToErrorResult();

        return TypedResults.Ok(mapper.Map<UserResponse>(result.Value));
    }
}
