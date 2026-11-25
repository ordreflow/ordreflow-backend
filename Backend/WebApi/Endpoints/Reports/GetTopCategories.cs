using Application;
using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Reports;

namespace WebAPI.Endpoints.Reports;

public sealed class GetTopCategories(IQueryDispatcher dispatcher)
    : ApiEndpoint.WithRequest<GetTopCategoriesRequest>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.TimeReports)]
    [HttpGet("reports/top-categories")]
    [ProducesResponseType(typeof(GetTopCategoriesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public override async Task<IResult> HandleAsync([FromQuery] GetTopCategoriesRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId)) return TypedResults.Unauthorized();
        var result = await dispatcher.DispatchAsync<GetTopCategoriesQuery, Result<IReadOnlyList<TopCategoryReportDto>>>(
            new(actorId, request.FromDate, request.ToDate));
        return result.IsFailure ? result.Errors.ToErrorResult()
            : TypedResults.Ok(new GetTopCategoriesResponse(result.Value.Select(category => new TopCategoryResponse(
                category.CategoryId, category.CategoryName, category.DepartmentId, category.DepartmentName, category.TotalHours,
                category.Tasks.Select(task => new TopCategoryTaskResponse(task.TaskId, task.TaskName, task.TotalHours)).ToArray())).ToArray()));
    }
}
