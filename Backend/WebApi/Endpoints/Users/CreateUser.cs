using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Users;

namespace WebAPI.endpoints.Users;

/// <summary>
/// Creates a user profile. Authentication credentials are managed separately.
/// </summary>
public class CreateUser
    : ApiEndpoint
        .WithRequest<CreateUserRequest>
        .AndResponse<IResult>
{
    [HttpPost("users")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public override Task<IResult> HandleAsync(
        [FromBody] CreateUserRequest request)
    {
        // TODO: Authentication/authorization
        // Resolve the current actor and require an active manager or admin.
        // The request must not contain a password; credentials belong to the
        // authentication subsystem (local Identity now, Entra later).

        // TODO: Application layer
        // Add CreateUserCommand and CreateUserHandler. The handler should
        // convert the request values to the Domain value objects and apply the
        // actor's user-management permissions.

        // TODO: Domain/Persistence layer
        // Use User.Create/CreateUser, persist the profile, and create/link its
        // authentication identity through the chosen authentication provider.

        // TODO: Response handling
        // Map the created User to UserResponse and return 201 Created with its
        // route location.

        _ = request;
        return Task.FromResult<IResult>(
            TypedResults.StatusCode(StatusCodes.Status501NotImplemented));
    }
}
