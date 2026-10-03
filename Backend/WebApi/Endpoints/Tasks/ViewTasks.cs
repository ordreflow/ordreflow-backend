using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Tasks;

namespace WebAPI.endpoints.Tasks;

/// <summary>
/// Lists tasks, optionally filtered by order ID.
/// </summary>
public class ViewTasks
    : ApiEndpoint
        .WithRequest<ViewTasksRequest>
        .AndResponse<IResult>
{
    [HttpGet("tasks")]
    [ProducesResponseType(typeof(ViewTasksResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public override Task<IResult> HandleAsync(
        [FromQuery] ViewTasksRequest request)
    {
        // TODO: Authentication/authorization
        // Apply the employee assignment/manager visibility policy once the
        // assignment model is defined.

        // TODO: Application/Persistence layer
        // Add a ViewTasksQuery and repository query using the optional OrderId.
        // Map WorkCase entities to ViewTasksResponse.

        _ = request;
        return Task.FromResult<IResult>(
            TypedResults.StatusCode(StatusCodes.Status501NotImplemented));
    }
}
