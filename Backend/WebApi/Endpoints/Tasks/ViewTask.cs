using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Tasks;

namespace WebAPI.endpoints.Tasks;

/// <summary>
/// Gets one task/work item by ID.
/// </summary>
public class ViewTask : EndpointBase
{
    [HttpGet("tasks/{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IResult HandleAsync([FromRoute] Guid id)
    {
        // TODO: Application/Persistence layer
        // Load the WorkCase by ID, enforce the actor's visibility/assignment
        // policy, map it to TaskResponse, and return 404 when it is not found.

        _ = id;
        return TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
    }
}
