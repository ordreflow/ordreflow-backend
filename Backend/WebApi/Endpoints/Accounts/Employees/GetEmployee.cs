using Application;
using Application.Accounts;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;

namespace WebAPI.Endpoints.Accounts.Employees;

/// <summary>Gets one employee account. Only administrators manage accounts.</summary>
public sealed class GetEmployee(IQueryDispatcher dispatcher)
    : ApiEndpoint.WithRequest<Guid>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.Employee)]
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpGet("employees/{id:guid}")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    public override async Task<IResult> HandleAsync([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<GetEmployeeQuery, Result<EmployeeDto>>(new(actor, id));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(result.Value);
    }
}
