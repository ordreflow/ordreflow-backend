using Application.Dtos;
using Application.Interfaces;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetTopCategoriesHandler(ITopCategoriesQuery reports, IEmployeeAccessRepository employees)
    : IQueryHandler<GetTopCategoriesQuery, Result<IReadOnlyList<TopCategoryReportDto>>>
{
    public async Task<Result<IReadOnlyList<TopCategoryReportDto>>> HandleAsync(GetTopCategoriesQuery query)
    {
        var id = EmployeeId.Create(query.ActorId);
        if (id.IsFailure) return Result<IReadOnlyList<TopCategoryReportDto>>.Failure(id.Errors.ToArray());
        var actor = await employees.GetWithAccessesAsync(id.Value);
        if (actor is null || !actor.CanViewTimeReports())
            return Result<IReadOnlyList<TopCategoryReportDto>>.Failure(
                new Error("ReportForbidden", "Active department-manager or administrator access is required."));
        if (query.FromDate == default || query.ToDate < query.FromDate)
            return Result<IReadOnlyList<TopCategoryReportDto>>.Failure(
                new Error("InvalidDateRange", "Select a valid inclusive work-date range."));

        var managerId = actor.HasPermission(SystemPermission.Administrator) ? null : actor.Id;
        return Result<IReadOnlyList<TopCategoryReportDto>>.Success(
            await reports.GetAsync(managerId, query.FromDate, query.ToDate));
    }
}
