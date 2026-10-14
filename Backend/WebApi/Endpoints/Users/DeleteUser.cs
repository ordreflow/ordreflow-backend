using Application;
using Application.Commands;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;

namespace WebAPI.endpoints.Users;

/// <summary>
/// Deactivates a user profile (soft delete). Admin only.
/// </summary>
public sealed class DeleteUser(
    ICommandDispatcher dispatcher)
    : ApiEndpoint
        .WithRequest<Guid>
        .AndResponse<IResult>
{
    [HttpDelete("users/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public override async Task<IResult> HandleAsync([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync(
            new DeactivateUserCommand(actorId, id));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Errors.ToErrorResult();
    }
}
