using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;

namespace WebAPI.endpoints.TimeEntries;

/// <summary>
/// Gets one time entry belonging to the authenticated employee.
/// </summary>
public class GetTimeEntry
    : ApiEndpoint
        .WithRequest<Guid>
        .AndResponse<IResult>
{
    [HttpGet("time_entries/{id:guid}")]
    
    [ProducesResponseType(typeof(TimeEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    
    public override Task<IResult> HandleAsync([FromRoute] Guid id)
    {
        // TODO: Application layer
        // 1. Add a GetTimeEntryQuery containing the entry ID and UserId.
        // 2. Inject the query dispatcher/read service and ICurrentUser.
        // 3. Execute the query using the authenticated UserId.

        // TODO: Domain/Persistence layer
        // Add a repository method that loads by entry ID while also enforcing
        // ownership through the current user's timesheet.

        // TODO: Response handling
        // Return TimeEntryResponse when found. Return 404 when the entry does
        // not exist or does not belong to the current user.

        _ = id;
        return Task.FromResult<IResult>(
            TypedResults.StatusCode(StatusCodes.Status501NotImplemented));
    }
}
