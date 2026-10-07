using Application;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;

namespace WebAPI.endpoints.Orders;

public sealed class ViewOrders(
    QueryDispatcher dispatcher)
    : ApiEndpoint
        .WithoutRequest
        .AndResponse<IResult>
{
    [HttpGet("orders")]
    public override async Task<IResult> HandleAsync()
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<
            GetOrdersQuery,
            Result<IReadOnlyList<Order>>>(
            new GetOrdersQuery(actorId.Value));

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.BadRequest(result.Errors);
    }
}