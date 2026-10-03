using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;

namespace WebAPI.endpoints.TimeEntries;

/// <summary>
/// Updates an editable time entry belonging to the authenticated employee.
/// </summary>
public class UpdateTimeEntry
    : ApiEndpoint
        .WithRequest<UpdateTimeEntryRequest>
        .AndResponse<IResult>
{
    [HttpPut("time_entries/{id:guid}")]
    
    [ProducesResponseType(typeof(TimeEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    
    public override Task<IResult> HandleAsync(
        [FromBody] UpdateTimeEntryRequest request)
    {
        // TODO: Application layer
        // 1. Add UpdateTimeEntryCommand containing the route ID, request data,
        //    and the authenticated UserId.
        // 2. Inject ICommandDispatcher and ICurrentUser.
        // 3. Dispatch the command and handle its Result.
        // 4. Extend the command/result flow to return the updated entry, or
        //    reload it after a successful update before creating the response.

        // TODO: Domain layer
        // Load the entry's timesheet and enforce ownership and status rules.
        // Only Draft/Rejected entries should be editable according to the
        // current draft contract. Apply date, hours, work-item, and comment
        // changes through domain methods rather than changing properties here.
        // The merged Domain currently treats TaskId as immutable, so changing
        // the task during an update needs an explicit Domain decision/method.

        // TODO: Persistence layer
        // Add the repository query/update methods and commit through the
        // agreed UnitOfWork boundary.

        // The route ID is available through RouteData.Values["id"]. It is kept
        // out of the body contract and will be parsed in the Application step.
        var routeId = RouteData.Values["id"];

        _ = routeId;
        _ = request;
        return Task.FromResult<IResult>(
            TypedResults.StatusCode(StatusCodes.Status501NotImplemented));
    }
}
