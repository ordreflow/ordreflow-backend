using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;

namespace WebAPI.endpoints.Users;

/// <summary>
/// Deactivates a user profile. This is planned as a soft delete.
/// </summary>
public class DeleteUser : EndpointBase
{
    [HttpDelete("users/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IResult HandleAsync([FromRoute] Guid id)
    {
        // TODO: Authentication/authorization
        // Require an active manager/admin and define whether an actor may
        // deactivate another manager/admin.

        // TODO: Application/Domain layer
        // Add DeactivateUserCommand. Load the User and call User.Deactivate();
        // do not hard-delete the profile while time sheets reference it.

        // TODO: Persistence layer
        // Save through the UnitOfWork and return 204 only after success.

        _ = id;
        return TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
    }
}
