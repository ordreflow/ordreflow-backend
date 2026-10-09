using Application;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Orders;

namespace WebAPI.endpoints.Orders;

public sealed class EditOrder(
    ICommandDispatcher dispatcher)
    : ApiEndpoint
        .WithRequest<UpdateOrderRequest>
        .AndResponse<IResult>
{
    [HttpPut("orders/{id:guid}")]
    public override async Task<IResult> HandleAsync(
        UpdateOrderRequest updateOrderRequest)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var orderId = Guid.Parse(RouteData.Values["id"]!.ToString()!);

        var result = await dispatcher.DispatchAsync(
            new RenameOrderCommand(
                OrderId: orderId,
                ActorId: actorId,
                Name: updateOrderRequest.Name));
        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.BadRequest(result.Errors);
    }
}
