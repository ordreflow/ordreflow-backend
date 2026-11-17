using Application;
using Application.Commands;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Common;
using WebAPI.Contracts.Users;
using Microsoft.AspNetCore.Authorization;
using WebAPI.Authentication;

namespace WebAPI.endpoints.Users;

/// <summary>
/// Sets or clears a user's displayed job title (FR-13). The title is descriptive only and does not change the
/// user's role or department access.
/// </summary>
public sealed class ChangeJobTitle(ICommandDispatcher dispatcher)
    : ApiEndpoint.WithRequest<ChangeJobTitleRequest>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("users/{id:guid}/jobTitle")]
    public override async Task<IResult> HandleAsync(ChangeJobTitleRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var adminId))
            return TypedResults.Unauthorized();

        var userId = Guid.Parse(RouteData.Values["id"]!.ToString()!);

        var result = await dispatcher.DispatchAsync(new ChangeJobTitleCommand(
            adminId,
            userId,
            request.JobTitle));

        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Errors.ToErrorResult();
    }
}
