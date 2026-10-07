using Application;
using Application.Commands;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Users;

namespace WebAPI.endpoints.Users;

public sealed class CreateUser(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<CreateUserRequest>.AndResponse<IResult>
{
    [HttpPost("users")]
    public override async Task<IResult> HandleAsync(CreateUserRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync(new CreateUserCommand(
            actorId.Value,
            request.Name,
            request.Email,
            request.Role));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.BadRequest(result.Errors);
    }
}
