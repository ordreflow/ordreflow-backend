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
/// Local employee accounts and their system access (UC-05: FR-05, FR-13, SEC-01). Only administrators manage
/// accounts; the displayed title never grants access.
/// </summary>
[Authorize(Policy = AuthorizationPolicies.Employee)]
public sealed class EmployeesController(ICommandDispatcher commands, IQueryDispatcher queries) : EndpointBase
{
    [HttpGet("employees/me")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    public async Task<IResult> Me()
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await queries.DispatchAsync<GetMyProfileQuery, Result<EmployeeDto>>(new(actor));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(result.Value);
    }

    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpGet("employees")]
    [ProducesResponseType(typeof(ItemsResponse<EmployeeDto>), StatusCodes.Status200OK)]
    public async Task<IResult> List()
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await queries.DispatchAsync<ListEmployeesQuery, Result<IReadOnlyList<EmployeeDto>>>(new(actor));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(new ItemsResponse<EmployeeDto>(result.Value));
    }

    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpGet("employees/{id:guid}")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    public async Task<IResult> Get([FromRoute] Guid id)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await queries.DispatchAsync<GetEmployeeQuery, Result<EmployeeDto>>(new(actor, id));
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.Ok(result.Value);
    }

    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpPost("employees")]
    [ProducesResponseType(typeof(CreatedResponse), StatusCodes.Status201Created)]
    public async Task<IResult> Create([FromBody] CreateEmployeeRequest request, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await commands.DispatchAsync<CreateEmployeeCommand, Guid>(new(
            actor, request.Name, request.Email, request.Password, request.Title, request.Permissions), ct);
        return result.IsFailure
            ? result.Errors.ToErrorResult()
            : TypedResults.Created($"/api/employees/{result.Value}", new CreatedResponse(result.Value));
    }

    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpPut("employees/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> Update([FromRoute] Guid id, [FromBody] UpdateEmployeeRequest request, CancellationToken ct) =>
        Run(actor => new UpdateEmployeeProfileCommand(actor, id, request.Name, request.Title), ct);

    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpPut("employees/{id:guid}/permissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> SetPermissions([FromRoute] Guid id, [FromBody] SetEmployeePermissionsRequest request, CancellationToken ct) =>
        Run(actor => new SetEmployeePermissionsCommand(actor, id, request.Permissions ?? []), ct);

    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpPost("employees/{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> Deactivate([FromRoute] Guid id, CancellationToken ct) =>
        Run(actor => new DeactivateEmployeeCommand(actor, id), ct);

    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    [HttpPost("employees/{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> Activate([FromRoute] Guid id, CancellationToken ct) =>
        Run(actor => new ActivateEmployeeCommand(actor, id), ct);

    private async Task<IResult> Run<TCommand>(Func<Guid, TCommand> create, CancellationToken ct)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actor)) return TypedResults.Unauthorized();
        var result = await commands.DispatchAsync<TCommand, bool>(create(actor), ct);
        return result.IsFailure ? result.Errors.ToErrorResult() : TypedResults.NoContent();
    }
}
