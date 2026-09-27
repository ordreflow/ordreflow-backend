using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Orders;

namespace WebAPI.endpoints.Orders;

/// <summary>
/// Renames an open order.
/// </summary>
public class EditOrder : EndpointBase
{
    [HttpPut("orders/{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IResult HandleAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateOrderRequest request)
    {
        // TODO: Application layer
        // Add RenameOrderCommand containing the route ID and current actor.

        // TODO: Domain layer
        // Load the Case and call Case.Rename. The Domain already prevents
        // renaming a closed case and checks manager/admin permissions.

        // TODO: Persistence/response handling
        // Save through UnitOfWork, map to OrderResponse, and return 404 when
        // the Case does not exist.

        _ = id;
        _ = request;
        return TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
    }
}
