using Application;
using Application.Interfaces;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Accounts;

namespace WebAPI.Endpoints.Accounts;

/// <summary>
/// Finding work to register time on (UC-09: FR-09, FR-21, FR-32, SEC-02, USE-04). Employees see general work in the
/// categories they may select and work assigned to them; assignment-only work stays hidden from everyone else,
/// also by direct URL.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.Employee)]
public sealed class WorkItemsController(IQueryDispatcher queries) : EndpointBase
{
    [HttpGet("work-items")]
    [ProducesResponseType(typeof(ItemsResponse<AccessibleWorkDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List([FromQuery] bool assignedToMe = false, [FromQuery] string? search = null)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await queries.DispatchAsync<ListAccessibleWorkQuery, Result<IReadOnlyList<AccessibleWorkDto>>>(
            new(actor, assignedToMe, search));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(new ItemsResponse<AccessibleWorkDto>(result.Value));
    }

    [HttpGet("work-items/{id:guid}")]
    [ProducesResponseType(typeof(AccessibleWorkDto), StatusCodes.Status200OK)]
    public async Task<IResult> Get([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await queries.DispatchAsync<GetAccessibleWorkQuery, Result<AccessibleWorkDto>>(new(actor, id));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(result.Value);
    }
}
