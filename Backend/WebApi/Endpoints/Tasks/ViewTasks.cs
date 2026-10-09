using Application;
using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Common;
using WebAPI.Contracts.Tasks;

namespace WebAPI.endpoints.Tasks;

/// <summary>
/// Lists tasks, optionally filtered by order ID.
/// </summary>
public sealed class ViewTasks(
    IQueryDispatcher dispatcher,
    IMapper mapper)
    : ApiEndpoint
        .WithRequest<ViewTasksRequest>
        .AndResponse<IResult>
{
    [HttpGet("tasks")]
    [ProducesResponseType(typeof(ViewTasksResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public override async Task<IResult> HandleAsync(
        [FromQuery] ViewTasksRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<ViewTasksQuery, Result<IReadOnlyList<TaskDto>>>(
            new ViewTasksQuery(actorId, request.OrderId));

        if (result.IsFailure)
            return result.Errors.ToErrorResult();

        return TypedResults.Ok(new ViewTasksResponse(result.Value
            .Select(mapper.Map<TaskResponse>)
            .ToArray()));
    }
}
