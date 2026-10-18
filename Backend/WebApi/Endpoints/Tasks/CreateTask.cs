using Application;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Tasks;
using Microsoft.AspNetCore.Authorization;
using WebAPI.Authentication;

namespace WebAPI.endpoints.Tasks;

public sealed class CreateTask(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<CreateTaskRequest>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
    [HttpPost("tasks")]
    public override async Task<IResult> HandleAsync(CreateTaskRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var managerId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync(new CreateTaskCommand(
            request.OrderId,
            managerId,
            request.Title,
            request.Description));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.BadRequest(result.Errors);
    }
}
