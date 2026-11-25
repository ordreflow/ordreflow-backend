using Application;
using Application.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Accounts;

namespace WebAPI.Endpoints.Accounts.WorkPlanning;

/// <summary>Creates a category in a department the caller manages (administrators may plan anywhere).</summary>
public sealed class CreateCategory(ICommandDispatcher dispatcher)
    : EndpointBase
{
    [Authorize(Policy = AuthorizationPolicies.WorkPlanning)]
    [HttpPost("departments/{id:guid}/categories")]
    [ProducesResponseType(typeof(CreatedResponse), StatusCodes.Status201Created)]
    public async Task<IResult> HandleAsync([FromRoute] Guid id, [FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<CreateCategoryCommand, Guid>(
            new(actor, id, request.Name, request.InternalOnly, request.Description, request.ReviewerIds), ct);
        return result.IsFailure
            ? result.Errors.ToErrorResult()
            : TypedResults.Created($"/api/categories/{result.Value}", new CreatedResponse(result.Value));
    }
}
