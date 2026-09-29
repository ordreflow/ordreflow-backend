using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;

namespace WebAPI.endpoints.Orders;

/// <summary>
/// Placeholder for the order lifecycle operation.
/// </summary>
public class DeleteOrder
    : ApiEndpoint
        .WithRequest<Guid>
        .AndResponse<IResult>
{
    [HttpDelete("orders/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public override Task<IResult> HandleAsync([FromRoute] Guid id)
    {
        // TODO: Product decision
        // The Domain has Close/Reopen, but no Case.Delete. Confirm whether
        // DELETE should close the order, soft-delete it, or be removed in favor
        // of a dedicated POST /orders/{id}/close action.

        // TODO: Application/Domain/Persistence layer
        // Implement the approved lifecycle command and return 204 only after
        // it succeeds.

        _ = id;
        return Task.FromResult<IResult>(
            TypedResults.StatusCode(StatusCodes.Status501NotImplemented));
    }
}
