using Application;
using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Authentication;
using WebAPI.Common;
using WebAPI.Contracts.Reports;

namespace WebAPI.endpoints.Reports;

public sealed class GetTopOrders(IQueryDispatcher dispatcher)
    : ApiEndpoint.WithRequest<GetTopOrdersRequest>.AndResponse<IResult>
{
    [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
    [HttpGet("reports/top-orders")]
    public override async Task<IResult> HandleAsync(
        [FromQuery] GetTopOrdersRequest request)
    {
        if (!HttpContext.TryGetCurrentUserId(out var actorId))
            return TypedResults.Unauthorized();

        var result = await dispatcher.DispatchAsync<
            GetTopOrdersQuery,
            Result<IReadOnlyList<TopOrdersReportDto>>>(
                new GetTopOrdersQuery(
                    actorId,
                    request.FromDate.ToDateTime(TimeOnly.MinValue),
                    request.ToDate.ToDateTime(TimeOnly.MaxValue)));

        if (result.IsFailure)
            return result.Errors.ToErrorResult();

        return TypedResults.Ok(new GetTopOrdersResponse(
            result.Value
                .Select(item => new TopOrdersResponse(
                    item.OrderId,
                    item.OrderName,
                    item.TotalHours))
                .ToArray()));
    }
}
