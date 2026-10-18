using Application;
using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Common;
using WebAPI.Contracts.Orders;
using Microsoft.AspNetCore.Authorization;
using WebAPI.Authentication;

namespace WebAPI.endpoints.Orders;

public sealed class ViewOrders(
    IQueryDispatcher dispatcher,
    IMapper mapper)
    : ApiEndpoint
        .WithoutRequest
        .AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.AnyRole)]
    [HttpGet("orders")]
    public override async Task<IResult> HandleAsync()
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<GetOrdersQuery, Result<IReadOnlyList<OrderDto>>>(
            new GetOrdersQuery(actorId));

        if (result.IsFailure)
            return TypedResults.BadRequest(result.Errors);

        return TypedResults.Ok(new ViewOrdersResponse(result.Value
            .Select(mapper.Map<OrderResponse>)
            .ToArray()));
    }
}