using Application;
using Application.Commands;
using Application.Handlers;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;

namespace WebAPI.endpoints.Orders;

public sealed class CloseOrder(
    ICommandDispatcher dispatcher)
    : ApiEndpoint
        .WithRequest<Guid>
        .AndResponse<IResult>
{
    [HttpPost("orders/{id:guid}/close")]
    public override async Task<IResult> HandleAsync(
        [FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync(
            new CloseOrderCommand(
                id,
                actorId));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.BadRequest(result.Errors);
    }
}
