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
/// Lists user profiles visible to the authenticated actor.
/// </summary>
public sealed class ViewUsers(
    IQueryDispatcher dispatcher,
    IMapper mapper)
    : ApiEndpoint
        .WithoutRequest
        .AndResponse<IResult>
{
    [HttpGet("users")]
    [ProducesResponseType(typeof(ViewUsersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public override async Task<IResult> HandleAsync()
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<GetUsersQuery, Result<IReadOnlyList<UserDto>>>(
            new GetUsersQuery(actorId));

        if (result.IsFailure)
            return result.Errors.ToErrorResult();

        return TypedResults.Ok(new ViewUsersResponse(result.Value
            .Select(mapper.Map<UserResponse>)
            .ToArray()));
    }
}
