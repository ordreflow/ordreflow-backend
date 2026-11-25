using Application;
using Application.Interfaces;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Accounts;

namespace WebAPI.Endpoints.Accounts.WorkItems;

/// <summary>
/// Finds work to register time on (UC-09: FR-09, FR-21, FR-32). Employees see general work in the categories they
/// may select and work assigned to them; assignment-only work stays hidden from everyone else.
/// </summary>
public sealed class ListWorkItems(IQueryDispatcher dispatcher)
    : ApiEndpoint.WithRequest<ListWorkItemsRequest>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.Employee)]
    [HttpGet("work-items")]
    [ProducesResponseType(typeof(ItemsResponse<AccessibleWorkDto>), StatusCodes.Status200OK)]
    public override async Task<IResult> HandleAsync([FromQuery] ListWorkItemsRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<ListAccessibleWorkQuery, Result<IReadOnlyList<AccessibleWorkDto>>>(
            new(actor, request.AssignedToMe, request.Search));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(new ItemsResponse<AccessibleWorkDto>(result.Value));
    }
}
