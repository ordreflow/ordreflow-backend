using Application;
using Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Accounts;

namespace WebAPI.Endpoints.Accounts.WorkPlanning;

/// <summary>Creates a project, ticket or task under a category the caller may plan in.</summary>
public sealed class CreateWorkItem(ICommandDispatcher dispatcher)
    : EndpointBase
{
    [Authorize(Policy = AuthorizationPolicies.WorkPlanning)]
    [HttpPost("categories/{id:guid}/work-items")]
    [ProducesResponseType(typeof(CreatedResponse), StatusCodes.Status201Created)]
    public async Task<IResult> HandleAsync([FromRoute] Guid id, [FromBody] CreateWorkItemRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<CreateWorkItemCommand, Guid>(
            new(actor, id, request.Name, request.Description, request.Kind, request.AssignmentOnly,
                request.Assignments?.Select(assignment =>
                    new WorkItemAssignmentInput(assignment.EmployeeId, assignment.ExpectedHours, assignment.ExpectedDeadline)).ToArray()), ct);
        return result.IsFailure
            ? result.Errors.ToErrorResult()
            : TypedResults.Created($"/api/work-items/{result.Value}", new CreatedResponse(result.Value));
    }
}
