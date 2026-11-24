using Application;
using Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Accounts;

namespace WebAPI.Endpoints.Accounts.Employees;

/// <summary>Creates a local employee account. Only administrators manage accounts.</summary>
public sealed class CreateEmployee(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<CreateEmployeeRequest>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.Employee)]
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpPost("employees")]
    [ProducesResponseType(typeof(CreatedResponse), StatusCodes.Status201Created)]
    public override async Task<IResult> HandleAsync([FromBody] CreateEmployeeRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<CreateEmployeeCommand, Guid>(new(
            actor, request.Name, request.Email, request.Password, request.Title, request.Permissions), HttpContext.RequestAborted);
        return result.IsFailure
            ? result.Errors.ToErrorResult()
            : TypedResults.Created($"/api/employees/{result.Value}", new CreatedResponse(result.Value));
    }
}
