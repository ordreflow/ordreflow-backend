using Application;
using Application.Accounts;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Accounts;

namespace WebAPI.Endpoints.Accounts.Employees;

/// <summary>Lists the employee accounts (UC-05: FR-05, SEC-01). Only administrators manage accounts.</summary>
public sealed class ListEmployees(IQueryDispatcher dispatcher)
    : ApiEndpoint.WithoutRequest.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.Employee)]
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpGet("employees")]
    [ProducesResponseType(typeof(ItemsResponse<EmployeeDto>), StatusCodes.Status200OK)]
    public override async Task<IResult> HandleAsync()
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<ListEmployeesQuery, Result<IReadOnlyList<EmployeeDto>>>(new(actor));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(new ItemsResponse<EmployeeDto>(result.Value));
    }
}
