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

/// <summary>The active employees in the departments the caller manages: who the caller may plan work for.</summary>
public sealed class ListManagedEmployees(IQueryDispatcher dispatcher)
    : ApiEndpoint.WithoutRequest.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.Employee)]
    [Authorize(Policy = AuthorizationPolicies.DepartmentManager)]
    [HttpGet("departments/managed-employees")]
    [ProducesResponseType(typeof(ItemsResponse<DepartmentPersonDto>), StatusCodes.Status200OK)]
    public override async Task<IResult> HandleAsync()
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<ListManagedEmployeesQuery, Result<IReadOnlyList<DepartmentPersonDto>>>(new(actor));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(new ItemsResponse<DepartmentPersonDto>(result.Value));
    }
}
