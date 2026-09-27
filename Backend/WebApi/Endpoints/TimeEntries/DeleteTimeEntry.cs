using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;

namespace WebAPI.endpoints.TimeEntries;

/// <summary>
/// Deletes an editable time entry belonging to the authenticated employee.
/// </summary>
public class DeleteTimeEntry : EndpointBase
{
    [HttpDelete("time_entries/{id:int}")]
    
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    
    public IResult HandleAsync([FromRoute] int id)
    {
        // TODO: Application layer
        // 1. Add DeleteTimeEntryCommand containing the route ID and the
        //    authenticated UserId.
        // 2. Inject ICommandDispatcher and ICurrentUser.
        // 3. Return 204 only after the command succeeds.

        // TODO: Domain layer
        // Enforce ownership and the deletion policy. The current draft allows
        // deletion only before approval, which should be represented by a
        // domain rule rather than checked in this endpoint.

        // TODO: Persistence layer
        // Add a repository delete method and commit through the agreed
        // UnitOfWork boundary.

        _ = id;
        return TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
    }
}
