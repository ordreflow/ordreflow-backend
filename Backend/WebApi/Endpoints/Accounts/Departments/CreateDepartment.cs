using Application;
using Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Accounts;

namespace WebAPI.Endpoints.Accounts.Departments;

/// <summary>Creates a department. Only administrators change departments.</summary>
public sealed class CreateDepartment(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<CreateDepartmentRequest>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.Employee)]
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpPost("departments")]
    [ProducesResponseType(typeof(CreatedResponse), StatusCodes.Status201Created)]
    public override async Task<IResult> HandleAsync([FromBody] CreateDepartmentRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<CreateDepartmentCommand, Guid>(
            new(actor, request.Name), HttpContext.RequestAborted);
        return result.IsFailure
            ? result.Errors.ToErrorResult()
            : TypedResults.Created($"/api/departments/{result.Value}", new CreatedResponse(result.Value));
    }
}
