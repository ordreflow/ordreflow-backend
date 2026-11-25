using Application;
using Application.Accounts;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;

namespace WebAPI.Endpoints.Accounts.Employees;

/// <summary>Gets the authenticated employee's own profile.</summary>
public sealed class GetMyProfile(IQueryDispatcher dispatcher)
    : ApiEndpoint.WithoutRequest.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.Employee)]
    [HttpGet("employees/me")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    public override async Task<IResult> HandleAsync()
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<GetMyProfileQuery, Result<EmployeeDto>>(new(actor));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(result.Value);
    }
}
