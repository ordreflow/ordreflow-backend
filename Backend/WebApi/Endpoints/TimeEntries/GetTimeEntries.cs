using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;

namespace WebAPI.endpoints.TimeEntries;

/// <summary>
/// Gets the authenticated employee's time entries for an inclusive date range.
/// </summary>
public class GetTimeEntries
    : ApiEndpoint
        .WithRequest<ViewTimeEntriesRequest>
        .AndResponse<IResult>
{
    [HttpGet("time_entries")]
    
    [ProducesResponseType(typeof(ViewTimeEntriesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    
    public override Task<IResult> HandleAsync(
        [FromQuery] ViewTimeEntriesRequest request)
    {
        // TODO: Application layer
        // 1. Add a GetTimeEntriesQuery containing the date range and UserId.
        // 2. Add a query dispatcher/read service. The current Application layer
        //    only has ICommandDispatcher, which is intended for state changes.
        // 3. Inject an ICurrentUser service and read the authenticated UserId.
        // 4. Dispatch the query with the current UserId, FromDate, and ToDate.

        // TODO: Domain/Persistence layer
        // Add a repository method that filters by both UserId and date range.
        // The client must not be able to choose another employee's UserId.

        // TODO: Response handling
        // Map the query result to ViewTimeEntriesResponse and map expected
        // validation/not-found/authorization failures to the agreed HTTP shape.

        _ = request;
        return Task.FromResult<IResult>(
            TypedResults.StatusCode(StatusCodes.Status501NotImplemented));
    }
}
