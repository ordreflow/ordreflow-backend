using Application;
using Application.Commands;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Users;

namespace WebAPI.endpoints.Users;

/// <summary>
/// Updates a user's profile fields (name, email). Role changes are handled
/// separately by the role-change endpoint.
/// </summary>
public sealed class EditUser(
    ICommandDispatcher dispatcher)
    : ApiEndpoint
        .WithRequest<UpdateUserRequest>
        .AndResponse<IResult>
{
    [HttpPut("users/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public override async Task<IResult> HandleAsync(
        [FromBody] UpdateUserRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var targetUserId = Guid.Parse(RouteData.Values["id"]!.ToString()!);

        var result = await dispatcher.DispatchAsync(
            new UpdateUserCommand(
                actorId,
                targetUserId,
                request.Name,
                request.Email));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Errors.ToErrorResult();
    }
}
