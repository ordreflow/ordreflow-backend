using Application.Dtos;

namespace Application.Interfaces;

public interface ITopOrdersRepository
{
    Task<IReadOnlyList<TopOrdersReportDto>> GetTopOrdersAsync(
        DateTime fromDate,
        DateTime toDate,
        int limit,
        bool onlyAccepted,
        CancellationToken cancellationToken = default);
}