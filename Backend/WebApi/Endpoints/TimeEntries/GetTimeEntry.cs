using Application;
using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Common;
using WebAPI.Contracts.TimeEntries;

namespace WebAPI.endpoints.TimeEntries;

/// <summary>
/// Gets one time entry, subject to owner/manager/admin authorization.
/// </summary>
public sealed class GetTimeEntry(
    IQueryDispatcher dispatcher,
    IMapper mapper)
    : ApiEndpoint
        .WithRequest<Guid>
        .AndResponse<IResult>
{
    [HttpGet("time_entries/{id:guid}")]
    [ProducesResponseType(typeof(TimeEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<Error>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public override async Task<IResult> HandleAsync([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<GetTimeEntryQuery, Result<TimeEntryDto>>(
            new GetTimeEntryQuery(actorId, id));

        if (result.IsFailure)
            return result.Errors.ToErrorResult();

        return TypedResults.Ok(mapper.Map<TimeEntryResponse>(result.Value));
    }
}
