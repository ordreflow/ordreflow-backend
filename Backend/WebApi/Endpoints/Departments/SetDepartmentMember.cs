using Application;
using Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Departments;

namespace WebAPI.endpoints.Departments;

/// <summary>
/// Adds a user to a department, or changes whether the member manages it (FR-14). Admin only. Only users with
/// manager or admin system access can be department managers; a job title never grants it (SEC-01).
/// </summary>
public sealed class SetDepartmentMember(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<SetDepartmentMemberRequest>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPut("departments/{id:guid}/members/{userId:guid}")]
    public override async Task<IResult> HandleAsync(SetDepartmentMemberRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var departmentId = Guid.Parse(RouteData.Values["id"]!.ToString()!);
        var userId = Guid.Parse(RouteData.Values["userId"]!.ToString()!);

        var result = await dispatcher.DispatchAsync(
            new SetDepartmentMemberCommand(actorId, departmentId, userId, request.IsManager));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Errors.ToErrorResult();
    }
}
