using Application;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Common;
using WebAPI.Contracts.Tasks;
using TaskEntity = Domain.Entities.Task;

namespace WebAPI.endpoints.Tasks;

public sealed class ViewMyTasks(
    IQueryHandler<GetMyTasksQuery, Result<IReadOnlyList<TaskEntity>>> queryHandler,
    IMapper mapper)
    : EndpointBase
{
    [HttpGet("tasks/me")]
    public async Task<IResult> HandleAsync()
    {
        if (!HttpContext.TryGetCurrentUserId(out var employeeId))
            return TypedResults.Unauthorized();

        var result = await queryHandler.HandleAsync(new GetMyTasksQuery(employeeId));
        if (result.IsFailure)
            return TypedResults.BadRequest(result.Errors);

        return TypedResults.Ok(new ViewTasksResponse(result.Value
            .Select(mapper.Map<TaskResponse>)
            .ToArray()));
    }
}
