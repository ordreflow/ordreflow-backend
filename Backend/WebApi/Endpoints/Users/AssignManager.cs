using Application;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Users;

namespace WebAPI.endpoints.Users;

public sealed class AssignManager(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<AssignManagerRequest>.AndResponse<IResult>
{
    [HttpPost("users/{id:guid}/manager")]
    public override async Task<IResult> HandleAsync(AssignManagerRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var adminId))
            return TypedResults.Unauthorized();

        var employeeId = Guid.Parse(RouteData.Values["id"]!.ToString()!);

        var result = await dispatcher.DispatchAsync(new AssignManagerCommand(
            adminId,
            employeeId,
            request.ManagerId));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.BadRequest(result.Errors);
    }
}
