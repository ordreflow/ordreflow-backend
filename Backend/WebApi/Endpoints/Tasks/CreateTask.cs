using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Tasks;

namespace WebAPI.endpoints.Tasks;

/// <summary>
/// Creates a task/work item under an order/case.
/// </summary>
public class CreateTask
    : ApiEndpoint
        .WithRequest<CreateTaskRequest>
        .AndResponse<IResult>
{
    [HttpPost("tasks")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public override Task<IResult> HandleAsync(
        [FromBody] CreateTaskRequest request)
    {
        // TODO: Authentication/authorization
        // Require an active manager/admin or whatever task-management policy
        // is approved for M2.

        // TODO: Application layer
        // Add CreateTaskCommand containing OrderId, Title, Description, and
        // the current actor identity.

        // TODO: Domain layer
        // Load the Case aggregate and call Case.AddWorkItem. This enforces
        // that the order is open and the task is attached only once.

        // TODO: Persistence/response handling
        // Persist the aggregate change, map WorkCase to TaskResponse, and
        // return 201 Created.

        _ = request;
        return Task.FromResult<IResult>(
            TypedResults.StatusCode(StatusCodes.Status501NotImplemented));
    }
}
