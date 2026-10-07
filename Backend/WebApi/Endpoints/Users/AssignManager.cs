using Application;
using Application.Commands;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Users;

namespace WebAPI.endpoints.Users;

public sealed class AssignManager(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<AssignManagerRequest>.AndResponse<IResult>
{
    [HttpPost("users/manager")]
    public override async Task<IResult> HandleAsync(AssignManagerRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var adminId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync(new AssignManagerCommand(
            adminId.Value,
            request.EmployeeId,
            request.ManagerId));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.BadRequest(result.Errors);
    }
}
