using Application;
using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;

namespace WebAPI.endpoints.TimeEntryReviews;

/// <summary>Gets the review history for one time entry visible to the current user.</summary>
public sealed class GetTimeEntryReviews(IQueryDispatcher dispatcher, IMapper mapper)
    : ApiEndpoint.WithRequest<Guid>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.AnyRole)]
    [HttpGet("time_entries/{id:guid}/reviews")]
    [ProducesResponseType(typeof(GetTimeEntryReviewsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public override async Task<IResult> HandleAsync([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<GetTimeEntryReviewsQuery, Result<IReadOnlyList<TimeEntryReviewDto>>>(
            new GetTimeEntryReviewsQuery(actorId, id));

        if (result.IsFailure)
            return result.Errors.ToErrorResult();

        return TypedResults.Ok(new GetTimeEntryReviewsResponse(result.Value
            .Select(mapper.Map<TimeEntryReviewResponse>)
            .ToArray()));
    }
}
