using Application;
using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Users;

namespace WebAPI.endpoints.Departments;

/// <summary>
/// Lists the active employees in the departments the current manager manages: exactly the employees the manager
/// may plan work for (FR-14).
/// </summary>
public sealed class ViewManagedEmployees(
    IQueryDispatcher dispatcher,
    IMapper mapper)
    : ApiEndpoint.WithoutRequest.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
    [HttpGet("departments/managed-employees")]
    public override async Task<IResult> HandleAsync()
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<GetManagedEmployeesQuery, Result<IReadOnlyList<UserDto>>>(
            new GetManagedEmployeesQuery(actorId));

        if (result.IsFailure)
            return result.Errors.ToErrorResult();

        return TypedResults.Ok(new ViewUsersResponse(result.Value
            .Select(mapper.Map<UserResponse>)
            .ToArray()));
    }
}
