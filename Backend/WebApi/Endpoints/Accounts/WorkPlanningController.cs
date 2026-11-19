using Application;
using Application.Commands;
using Application.Queries;
using Application.WorkPlanning;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Accounts;

namespace WebAPI.Endpoints.Accounts;

/// <summary>
/// Planning work (UC-08: FR-08, SEC-08). Department managers create categories and projects, tickets or tasks
/// only in departments they manage; administrators may plan anywhere. Work cannot be moved between categories.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.WorkPlanning)]
public sealed class WorkPlanningController(ICommandDispatcher commands, IQueryDispatcher queries) : EndpointBase
{
    [HttpGet("categories/plannable")]
    [ProducesResponseType(typeof(ItemsResponse<PlannableCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IResult> Plannable()
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await queries.DispatchAsync<ListPlannableCategoriesQuery, Result<IReadOnlyList<PlannableCategoryDto>>>(new(actor));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(new ItemsResponse<PlannableCategoryDto>(result.Value));
    }

    [HttpPost("departments/{id:guid}/categories")]
    [ProducesResponseType(typeof(CreatedResponse), StatusCodes.Status201Created)]
    public async Task<IResult> CreateCategory([FromRoute] Guid id, [FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await commands.DispatchAsync<CreateCategoryCommand, Guid>(
            new(actor, id, request.Name, request.InternalOnly, request.Description), ct);
        return result.IsFailure
            ? result.Errors.ToErrorResult()
            : TypedResults.Created($"/api/categories/{result.Value}", new CreatedResponse(result.Value));
    }

    [HttpPost("categories/{id:guid}/work-items")]
    [ProducesResponseType(typeof(CreatedResponse), StatusCodes.Status201Created)]
    public async Task<IResult> CreateWorkItem([FromRoute] Guid id, [FromBody] CreateWorkItemRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await commands.DispatchAsync<CreateWorkItemCommand, Guid>(
            new(actor, id, request.Name, request.Description, request.Kind, request.AssignmentOnly,
                request.Assignments?.Select(assignment =>
                    new WorkItemAssignmentInput(assignment.EmployeeId, assignment.ExpectedHours, assignment.ExpectedDeadline)).ToArray()), ct);
        return result.IsFailure
            ? result.Errors.ToErrorResult()
            : TypedResults.Created($"/api/work-items/{result.Value}", new CreatedResponse(result.Value));
    }

    [HttpPut("categories/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> UpdateCategory([FromRoute] Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await commands.DispatchAsync<UpdateCategoryCommand, bool>(
            new(actor, id, request.Name, request.Description), ct);
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.NoContent();
    }

    [HttpPut("work-items/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> UpdateWorkItem([FromRoute] Guid id, [FromBody] UpdateWorkItemRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await commands.DispatchAsync<UpdateWorkItemCommand, bool>(
            new(actor, id, request.Name, request.Description, request.Kind, request.AssignmentOnly,
                request.Assignments?.Select(assignment =>
                    new WorkItemAssignmentInput(assignment.EmployeeId, assignment.ExpectedHours, assignment.ExpectedDeadline)).ToArray()), ct);
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.NoContent();
    }
}
