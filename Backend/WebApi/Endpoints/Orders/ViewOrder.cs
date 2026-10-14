using Application;
using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Common;
using WebAPI.Contracts.Orders;

namespace WebAPI.endpoints.Orders;

public sealed class ViewOrder(
    IQueryDispatcher dispatcher,
    IMapper mapper)
    : ApiEndpoint
        .WithRequest<Guid>
        .AndResponse<IResult>
{
    [HttpGet("orders/{id:guid}")]
    public override async Task<IResult> HandleAsync(
        [FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<GetOrderQuery, Result<OrderDto>>(
            new GetOrderQuery(actorId, id));

        if (result.IsFailure)
            return result.Errors.ToErrorResult();

        return TypedResults.Ok(mapper.Map<OrderResponse>(result.Value));
    }
}
