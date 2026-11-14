using Application;
using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;
using Microsoft.AspNetCore.Authorization;
using WebAPI.Authentication;

namespace WebAPI.endpoints.TimeEntries;

public sealed class GetReviewerTimeEntries(
    IQueryDispatcher dispatcher,
    IMapper mapper)
    : ApiEndpoint.WithRequest<ViewTimeEntriesRequest>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.CatagoryReviewer)]
    [HttpGet("time_entries/reviewer")]
    [ProducesResponseType(typeof(ViewTimeEntriesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public override async Task<IResult> HandleAsync([FromQuery] ViewTimeEntriesRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var reviewerId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<GetReviewerTimeEntriesQuery, Result<IReadOnlyList<TimeEntryDto>>>(
            new GetReviewerTimeEntriesQuery(
                reviewerId,
                request.FromDate.ToDateTime(TimeOnly.MinValue),
                request.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
                null));

        if (result.IsFailure)
            return result.Errors.ToErrorResult();

        return TypedResults.Ok(new ViewTimeEntriesResponse(result.Value
            .Select(mapper.Map<TimeEntryResponse>)
            .ToArray()));
    }
}
