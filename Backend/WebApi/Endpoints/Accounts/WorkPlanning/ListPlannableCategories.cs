using Application;
using Application.Queries;
using Application.WorkPlanning;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Accounts;

namespace WebAPI.Endpoints.Accounts.WorkPlanning;

/// <summary>Lists the categories the caller may plan work in (UC-08: FR-08, SEC-08).</summary>
public sealed class ListPlannableCategories(IQueryDispatcher dispatcher)
    : ApiEndpoint.WithoutRequest.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.WorkPlanning)]
    [HttpGet("categories/plannable")]
    [ProducesResponseType(typeof(ItemsResponse<PlannableCategoryDto>), StatusCodes.Status200OK)]
    public override async Task<IResult> HandleAsync()
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<ListPlannableCategoriesQuery, Result<IReadOnlyList<PlannableCategoryDto>>>(new(actor));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(new ItemsResponse<PlannableCategoryDto>(result.Value));
    }
}
