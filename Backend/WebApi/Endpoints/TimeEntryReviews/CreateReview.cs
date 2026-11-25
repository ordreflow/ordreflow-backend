using Application;
using Application.Commands;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;
using Microsoft.AspNetCore.Authorization;
using WebAPI.Authentication;

namespace WebAPI.endpoints.TimeEntryReviews;

public sealed class CreateReview(ICommandDispatcher dispatcher)
    : EndpointBase
{
    [Authorize(Policy = AuthorizationPolicies.CatagoryReviewer)]
    [HttpPost("time_entries/{id:guid}/review")]
    public async Task<IResult> HandleAsync(
        [FromRoute] Guid id,
        [FromBody] ReviewTimeEntryRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var reviewerId))
            return TypedResults.Unauthorized();

        if (string.Equals(request.Decision, "accept", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(request.Decision, "approve", StringComparison.OrdinalIgnoreCase))
        {
            var result = await dispatcher.DispatchAsync(
                new ApproveTimeEntryCommand(id, reviewerId));
            return result.IsSuccess
                ? TypedResults.NoContent()
                : result.Errors.ToErrorResult();
        }

        if (string.Equals(request.Decision, "return", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(request.Decision, "reject", StringComparison.OrdinalIgnoreCase))
        {
            var result = await dispatcher.DispatchAsync(
                new RejectTimeEntryCommand(id, reviewerId, request.Reason));
            return result.IsSuccess
                ? TypedResults.NoContent()
                : result.Errors.ToErrorResult();
        }

        return TypedResults.BadRequest(new[] {
            new Error("InvalidDecision", "Decision must be approve or reject.")
        });
    }
}
