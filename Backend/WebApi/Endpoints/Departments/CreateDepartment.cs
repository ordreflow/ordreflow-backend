using Application;
using Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Departments;

namespace WebAPI.endpoints.Departments;

/// <summary>Creates a department (FR-14). Admin only.</summary>
public sealed class CreateDepartment(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<CreateDepartmentRequest>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("departments")]
    public override async Task<IResult> HandleAsync(CreateDepartmentRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync(new CreateDepartmentCommand(actorId, request.Name));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Errors.ToErrorResult();
    }
}
