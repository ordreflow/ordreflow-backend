using Application;
using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Common;
using WebAPI.Contracts.Tasks;

namespace WebAPI.endpoints.Tasks;

public sealed class ViewMyTasks(
    IQueryDispatcher dispatcher,
    IMapper mapper)
    : EndpointBase
{
    [HttpGet("tasks/me")]
    public async Task<IResult> HandleAsync()
    {
        if (!HttpContext.TryGetCurrentUserId(out var employeeId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<GetMyTasksQuery, Result<IReadOnlyList<TaskDto>>>(
            new GetMyTasksQuery(employeeId));
        if (result.IsFailure)
            return TypedResults.BadRequest(result.Errors);

        return TypedResults.Ok(new ViewTasksResponse(result.Value
            .Select(mapper.Map<TaskResponse>)
            .ToArray()));
    }
}
