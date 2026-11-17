using Application;
using Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Departments;

namespace WebAPI.endpoints.Departments;

/// <summary>Removes a user from a department. Admin only.</summary>
public sealed class RemoveDepartmentMember(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithoutRequest.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpDelete("departments/{id:guid}/members/{userId:guid}")]
    public override async Task<IResult> HandleAsync()
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var departmentId = Guid.Parse(RouteData.Values["id"]!.ToString()!);
        var userId = Guid.Parse(RouteData.Values["userId"]!.ToString()!);

        var result = await dispatcher.DispatchAsync(
            new RemoveDepartmentMemberCommand(actorId, departmentId, userId));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Errors.ToErrorResult();
    }
}
