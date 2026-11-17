using Application;
using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ObjectMapper;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Departments;

namespace WebAPI.endpoints.Departments;

/// <summary>
/// Lists departments. Admins see all departments and members; managers see the members of departments they manage;
/// other users only see the departments they belong to.
/// </summary>
public sealed class ViewDepartments(
    IQueryDispatcher dispatcher,
    IMapper mapper)
    : ApiEndpoint.WithoutRequest.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.AnyRole)]
    [HttpGet("departments")]
    public override async Task<IResult> HandleAsync()
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<GetDepartmentsQuery, Result<IReadOnlyList<DepartmentDto>>>(
            new GetDepartmentsQuery(actorId));

        if (result.IsFailure)
            return result.Errors.ToErrorResult();

        return TypedResults.Ok(new ViewDepartmentsResponse(result.Value
            .Select(mapper.Map<DepartmentResponse>)
            .ToArray()));
    }
}
