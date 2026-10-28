using Application.Dtos;
using Domain.ValueObjects;

namespace Application.Interfaces;

public interface ITopOrdersRepository
{
    Task<IReadOnlyList<TopOrdersReportDto>> GetTopOrdersAsync(
        UserId? managerId,
        DateTime fromDate,
        DateTime toDate,
        int limit,
        bool onlyAccepted,
        CancellationToken cancellationToken = default);
}