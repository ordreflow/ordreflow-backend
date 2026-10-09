using Application;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Users;

namespace WebAPI.endpoints.Users;

public sealed class ChangeUserRole(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<ChangeUserRoleRequest>.AndResponse<IResult>
{
    [HttpPost("users/{id:guid}/role")]
    public override async Task<IResult> HandleAsync(ChangeUserRoleRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var adminId))
            return TypedResults.Unauthorized();

        var userId = Guid.Parse(RouteData.Values["id"]!.ToString()!);

        var result = await dispatcher.DispatchAsync(new ChangeUserRoleCommand(
            adminId,
            userId,
            request.Role));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.BadRequest(result.Errors);
    }
}
