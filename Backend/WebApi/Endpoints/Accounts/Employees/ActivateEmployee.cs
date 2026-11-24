using Application;
using Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;

namespace WebAPI.Endpoints.Accounts.Employees;

/// <summary>Activates an employee account. Only administrators manage accounts.</summary>
public sealed class ActivateEmployee(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<Guid>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.Employee)]
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpPost("employees/{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public override async Task<IResult> HandleAsync([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<ActivateEmployeeCommand, bool>(
            new ActivateEmployeeCommand(actor, id), HttpContext.RequestAborted);
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.NoContent();
    }
}
