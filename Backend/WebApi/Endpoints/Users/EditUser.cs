using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Users;

namespace WebAPI.endpoints.Users;

/// <summary>
/// Updates a user's profile fields.
/// </summary>
public class EditUser : EndpointBase
{
    [HttpPut("users/{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IResult HandleAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateUserRequest request)
    {
        // TODO: Authentication/authorization
        // Require the actor to be allowed to edit this user. Role changes are
        // intentionally not part of this profile update contract.

        // TODO: Application layer
        // Add UpdateUserCommand containing the route ID, request values, and
        // current actor identity. Dispatch it through the Application layer.

        // TODO: Domain layer
        // Load User and call ChangeName/ChangeEmail rather than mutating the
        // aggregate directly.

        // TODO: Persistence/response handling
        // Save through the UnitOfWork, map the updated profile to UserResponse,
        // and return 404 when the user cannot be found.

        _ = id;
        _ = request;
        return TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
    }
}
