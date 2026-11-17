using Application;
using Application.Accounts;
using Application.Commands;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Accounts;

namespace WebAPI.Endpoints.Accounts;

/// <summary>
/// Departments, their members and department managers (UC-05: FR-14, SEC-01). A user can belong to and manage
/// several departments. Only administrators change departments.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.Employee)]
public sealed class DepartmentsController(ICommandDispatcher commands, IQueryDispatcher queries) : EndpointBase
{
    [HttpGet("departments")]
    [ProducesResponseType(typeof(ItemsResponse<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List()
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await queries.DispatchAsync<ListDepartmentsQuery, Result<IReadOnlyList<DepartmentDto>>>(new(actor));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(new ItemsResponse<DepartmentDto>(result.Value));
    }

    /// <summary>The active employees in the departments the caller manages: who the caller may plan work for.</summary>
    [Authorize(Policy = AuthorizationPolicies.DepartmentManager)]
    [HttpGet("departments/managed-employees")]
    [ProducesResponseType(typeof(ItemsResponse<DepartmentPersonDto>), StatusCodes.Status200OK)]
    public async Task<IResult> ManagedEmployees()
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await queries.DispatchAsync<ListManagedEmployeesQuery, Result<IReadOnlyList<DepartmentPersonDto>>>(new(actor));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(new ItemsResponse<DepartmentPersonDto>(result.Value));
    }

    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpPost("departments")]
    [ProducesResponseType(typeof(CreatedResponse), StatusCodes.Status201Created)]
    public async Task<IResult> Create([FromBody] CreateDepartmentRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await commands.DispatchAsync<CreateDepartmentCommand, Guid>(new(actor, request.Name), ct);
        return result.IsFailure
            ? result.Errors.ToErrorResult()
            : TypedResults.Created($"/api/departments/{result.Value}", new CreatedResponse(result.Value));
    }

    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpPut("departments/{id:guid}/employees/{employeeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> AddEmployee([FromRoute] Guid id, [FromRoute] Guid employeeId, CancellationToken ct) =>
        Run(actor => new AddDepartmentEmployeeCommand(actor, id, employeeId), ct);

    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpDelete("departments/{id:guid}/employees/{employeeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> RemoveEmployee([FromRoute] Guid id, [FromRoute] Guid employeeId, CancellationToken ct) =>
        Run(actor => new RemoveDepartmentEmployeeCommand(actor, id, employeeId), ct);

    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpPut("departments/{id:guid}/managers/{employeeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> AppointManager([FromRoute] Guid id, [FromRoute] Guid employeeId, CancellationToken ct) =>
        Run(actor => new AppointDepartmentManagerCommand(actor, id, employeeId), ct);

    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpDelete("departments/{id:guid}/managers/{employeeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> RemoveManager([FromRoute] Guid id, [FromRoute] Guid employeeId, CancellationToken ct) =>
        Run(actor => new RemoveDepartmentManagerCommand(actor, id, employeeId), ct);

    private async Task<IResult> Run<TCommand>(Func<Guid, TCommand> create, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await commands.DispatchAsync<TCommand, bool>(create(actor), ct);
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.NoContent();
    }
}
