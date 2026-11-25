using Application;
using Application.Interfaces;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;

namespace WebAPI.Endpoints.Accounts.WorkItems;

/// <summary>Gets one work item the caller may access. Assignment-only work stays hidden, also by direct URL.</summary>
public sealed class GetWorkItem(IQueryDispatcher dispatcher)
    : ApiEndpoint.WithRequest<Guid>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.Employee)]
    [HttpGet("work-items/{id:guid}")]
    [ProducesResponseType(typeof(AccessibleWorkDto), StatusCodes.Status200OK)]
    public override async Task<IResult> HandleAsync([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<GetAccessibleWorkQuery, Result<AccessibleWorkDto>>(new(actor, id));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(result.Value);
    }
}
