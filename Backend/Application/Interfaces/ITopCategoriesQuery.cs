using Application.Dtos;
using Domain.ValueObjects;

namespace Application.Interfaces;

public interface ITopCategoriesQuery
{
    // Null means company-wide administrator scope. A manager ID is always scoped by
    // current DepartmentManager links, never by job title or employee membership.
    Task<IReadOnlyList<TopCategoryReportDto>> GetAsync(EmployeeId? managerId, DateOnly fromDate,
        DateOnly toDate, CancellationToken cancellationToken = default);
}
