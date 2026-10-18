using Application;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Orders;
using Microsoft.AspNetCore.Authorization;
using WebAPI.Authentication;

namespace WebAPI.endpoints.Orders;

public sealed class CreateOrder(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<CreateOrderRequest>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
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
