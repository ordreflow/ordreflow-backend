using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Users;

namespace WebAPI.endpoints.Users;

/// <summary>
/// Lists user profiles visible to the authenticated actor.
/// </summary>
public class ViewUsers
    : ApiEndpoint
        .WithoutRequest
        .AndResponse<IResult>
{
    [HttpGet("users")]
    [ProducesResponseType(typeof(ViewUsersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public override Task<IResult> HandleAsync()
    {
        // TODO: Authentication/authorization
        // Decide whether employees may see this collection or whether it is
        // restricted to active managers/admins.

        // TODO: Application layer
        // Add a ViewUsersQuery and query/read service. Resolve the current
        // actor so the handler can apply the correct visibility scope.

        // TODO: Persistence layer
        // Add a repository query that returns user profiles without password
        // hashes, tokens, or other authentication data.

        // TODO: Response handling
        // Map the result collection to ViewUsersResponse.

        return Task.FromResult<IResult>(
            TypedResults.StatusCode(StatusCodes.Status501NotImplemented));
    }
}
