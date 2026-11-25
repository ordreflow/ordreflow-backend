using Application;
using Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;

namespace WebAPI.Endpoints.Accounts.Departments;

/// <summary>Appoints an employee as manager of a department.</summary>
public sealed class AppointDepartmentManager(ICommandDispatcher dispatcher)
    : EndpointBase
{
    [Authorize(Policy = AuthorizationPolicies.Employee)]
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpPut("departments/{id:guid}/managers/{employeeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> HandleAsync([FromRoute] Guid id, [FromRoute] Guid employeeId, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<AppointDepartmentManagerCommand, bool>(new AppointDepartmentManagerCommand(actor, id, employeeId), ct);
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.NoContent();
    }
}
