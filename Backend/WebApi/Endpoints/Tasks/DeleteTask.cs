using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;

namespace WebAPI.endpoints.Tasks;

/// <summary>
/// Removes a task/work item from its order/case.
/// </summary>
public class DeleteTask : EndpointBase
{
    [HttpDelete("tasks/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IResult HandleAsync([FromRoute] Guid id)
    {
        // TODO: Authentication/authorization
        // Require the approved task-management permission.

        // TODO: Application/Domain layer
        // Add RemoveTaskCommand. Load the owning Case and call
        // Case.RemoveWorkItem so the aggregate can enforce its rules.

        // TODO: Persistence layer
        // Commit the removal through the UnitOfWork and return 204 only after
        // the operation succeeds.

        _ = id;
        return TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
    }
}
