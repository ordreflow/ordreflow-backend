using Application;
using Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Departments;

namespace WebAPI.endpoints.Departments;

/// <summary>Renames a department. Admin only.</summary>
public sealed class RenameDepartment(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<RenameDepartmentRequest>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPut("departments/{id:guid}")]
    public override async Task<IResult> HandleAsync(RenameDepartmentRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var departmentId = Guid.Parse(RouteData.Values["id"]!.ToString()!);

        var result = await dispatcher.DispatchAsync(
            new RenameDepartmentCommand(actorId, departmentId, request.Name));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Errors.ToErrorResult();
    }
}
