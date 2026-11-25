using Application;
using Application.Accounts;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Accounts;

namespace WebAPI.Endpoints.Accounts.Departments;

/// <summary>Lists the departments (UC-05: FR-14, SEC-01).</summary>
public sealed class ListDepartments(IQueryDispatcher dispatcher)
    : ApiEndpoint.WithoutRequest.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.Employee)]
    [HttpGet("departments")]
    [ProducesResponseType(typeof(ItemsResponse<DepartmentDto>), StatusCodes.Status200OK)]
    public override async Task<IResult> HandleAsync()
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<ListDepartmentsQuery, Result<IReadOnlyList<DepartmentDto>>>(new(actor));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(new ItemsResponse<DepartmentDto>(result.Value));
    }
}
