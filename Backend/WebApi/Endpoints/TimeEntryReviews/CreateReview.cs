using Application;
using Application.Commands;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;

namespace WebAPI.endpoints.TimeEntryReviews;

public sealed class CreateReview(ICommandDispatcher dispatcher)
    : EndpointBase
{
    [HttpPost("time_entries/{id:guid}/review")]
    public async Task<IResult> HandleAsync(
        [FromRoute] Guid id,
        [FromBody] ReviewTimeEntryRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var reviewerId))
            return TypedResults.Unauthorized();

        if (string.Equals(request.Decision, "accept", StringComparison.OrdinalIgnoreCase))
        {
            var result = await dispatcher.DispatchAsync(
                new AcceptTimeEntryCommand(id, reviewerId.Value));
            return result.IsSuccess
                ? TypedResults.NoContent()
                : TypedResults.BadRequest(result.Errors);
        }

        if (string.Equals(request.Decision, "return", StringComparison.OrdinalIgnoreCase))
        {
            var result = await dispatcher.DispatchAsync(
                new ReturnTimeEntryCommand(id, reviewerId.Value, request.Reason ?? string.Empty));
            return result.IsSuccess
                ? TypedResults.NoContent()
                : TypedResults.BadRequest(result.Errors);
        }

        if (string.Equals(request.Decision, "finalize", StringComparison.OrdinalIgnoreCase))
        {
            var result = await dispatcher.DispatchAsync(
                new FinalizeTimeEntryCommand(id, reviewerId.Value));
            return result.IsSuccess
                ? TypedResults.NoContent()
                : TypedResults.BadRequest(result.Errors);
        }

        return TypedResults.BadRequest(new[] {
            new Error("InvalidDecision", "Decision must be accept, return, or finalize.")
        });
    }
}
