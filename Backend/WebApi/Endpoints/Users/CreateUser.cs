using Application;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using WebAPI.Authentication;

namespace WebAPI.endpoints.Users;

public sealed class CreateUser(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<CreateUserRequest>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("users")]
    public override async Task<IResult> HandleAsync(CreateUserRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync(new CreateUserCommand(
            actorId,
            request.Name,
            request.Email,
            request.Password,
            request.Role,
            request.ManagerId));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.BadRequest(result.Errors);
    }
}
