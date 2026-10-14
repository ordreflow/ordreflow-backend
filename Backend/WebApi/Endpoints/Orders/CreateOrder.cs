using Application;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Orders;

namespace WebAPI.endpoints.Orders;

public sealed class CreateOrder(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<CreateOrderRequest>.AndResponse<IResult>
{
    [HttpPost("orders")]
    public override async Task<IResult> HandleAsync(CreateOrderRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var managerId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync(
            new CreateOrderCommand(managerId, request.Name));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.BadRequest(result.Errors);
    }
}
