using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Orders;

namespace WebAPI.endpoints.Orders;

/// <summary>
/// Lists orders visible to the authenticated actor.
/// </summary>
public class ViewOrders : EndpointBase
{
    [HttpGet("orders")]
    [ProducesResponseType(typeof(ViewOrdersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IResult HandleAsync()
    {
        // TODO: Authentication/authorization
        // Define whether employees see all orders or only assigned orders.
        // The current Domain model does not yet contain an assignment table.

        // TODO: Application/Persistence layer
        // Add a ViewOrdersQuery and repository query with the agreed visibility
        // scope, then map the Cases to OrderResponse values.

        return TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
    }
}
