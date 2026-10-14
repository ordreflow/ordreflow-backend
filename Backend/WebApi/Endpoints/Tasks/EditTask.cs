using Application;
using Application.Commands;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Tasks;

namespace WebAPI.endpoints.Tasks;

/// <summary>
/// Updates a task/work item.
/// </summary>
public sealed class EditTask(
    ICommandDispatcher dispatcher)
    : ApiEndpoint
        .WithRequest<UpdateTaskRequest>
        .AndResponse<IResult>
{
    [HttpPut("tasks/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public override async Task<IResult> HandleAsync(
        [FromBody] UpdateTaskRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var taskId = Guid.Parse(RouteData.Values["id"]!.ToString()!);

        var result = await dispatcher.DispatchAsync(
            new UpdateTaskCommand(
                actorId,
                taskId,
                request.Title,
                request.Description));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Errors.ToErrorResult();
    }
}
