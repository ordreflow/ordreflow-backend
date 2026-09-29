using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Orders;

namespace WebAPI.endpoints.Orders;

/// <summary>
/// Creates an order. The Domain currently calls this concept a Case.
/// </summary>
public class CreateOrder
    : ApiEndpoint
        .WithRequest<CreateOrderRequest>
        .AndResponse<IResult>
{
    [HttpPost("orders")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public override Task<IResult> HandleAsync(
        [FromBody] CreateOrderRequest request)
    {
        // TODO: Authentication/authorization
        // Resolve the current actor and require an active manager/admin.

        // TODO: Application layer
        // Add CreateOrderCommand and handler. Map the API name "Order" to the
        // Domain Case aggregate and include the actor's role/status.

        // TODO: Domain/Persistence layer
        // Call Case.Create, persist the aggregate, and commit through the
        // UnitOfWork.

        // TODO: Response handling
        // Map Case to OrderResponse and return 201 Created.

        _ = request;
        return Task.FromResult<IResult>(
            TypedResults.StatusCode(StatusCodes.Status501NotImplemented));
    }
}
