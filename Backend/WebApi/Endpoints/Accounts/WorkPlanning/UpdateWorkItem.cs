using Application;
using Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Accounts;

namespace WebAPI.Endpoints.Accounts.WorkPlanning;

/// <summary>Replaces a work item's details and its assignments.</summary>
public sealed class UpdateWorkItem(ICommandDispatcher dispatcher)
    : EndpointBase
{
    [Authorize(Policy = AuthorizationPolicies.WorkPlanning)]
    [HttpPut("work-items/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> HandleAsync([FromRoute] Guid id, [FromBody] UpdateWorkItemRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<UpdateWorkItemCommand, bool>(
            new(actor, id, request.Name, request.Description, request.Kind, request.AssignmentOnly,
                request.Assignments?.Select(assignment =>
                    new WorkItemAssignmentInput(assignment.EmployeeId, assignment.ExpectedHours, assignment.ExpectedDeadline)).ToArray()), ct);
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.NoContent();
    }
}
