using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Tasks;

namespace WebAPI.endpoints.Tasks;

/// <summary>
/// Updates a task/work item.
/// </summary>
public class EditTask
    : ApiEndpoint
        .WithRequest<UpdateTaskRequest>
        .AndResponse<IResult>
{
    [HttpPut("tasks/{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public override Task<IResult> HandleAsync(
        [FromBody] UpdateTaskRequest request)
    {
        // TODO: Authentication/authorization
        // Require the approved task-management permission.

        // TODO: Application layer
        // Add UpdateTaskCommand with the route ID, request fields, and actor.

        // TODO: Domain layer
        // Load the owning Case/WorkCase and call ChangeTitle and
        // ChangeDescription. Moving a task between orders is not supported by
        // the current Domain model and needs a separate decision/method.

        // TODO: Persistence/response handling
        // Save through UnitOfWork, map to TaskResponse, and return 404 when
        // the task is not found.

        // The route ID is available through RouteData.Values["id"]. It is kept
        // out of the body contract and will be parsed in the Application step.
        var routeId = RouteData.Values["id"];

        _ = routeId;
        _ = request;
        return Task.FromResult<IResult>(
            TypedResults.StatusCode(StatusCodes.Status501NotImplemented));
    }
}
