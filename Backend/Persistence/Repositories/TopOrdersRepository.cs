using Application.Dtos;
using Application.Interfaces;
using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public sealed class TopOrdersRepository(AppDbContext context) : ITopOrdersRepository
{
    public async Task<IReadOnlyList<TopOrdersReportDto>> GetTopOrdersAsync(
        UserId? managerId,
        DateTime fromDate,
        DateTime toDate,
        int limit,
        bool onlyAccepted,
        CancellationToken cancellationToken = default)
    {
        var entries = context.TimeEntries
            .AsNoTracking()
            .Where(entry => entry.Date >= fromDate && entry.Date <= toDate);

        if (onlyAccepted)
        {
            entries = entries.Where(entry =>
                entry.Status == TimeEntryStatus.Accepted ||
                entry.Status == TimeEntryStatus.Finalized);
        }

        IQueryable<Order> orders = context.Orders.AsNoTracking();
        if (managerId is not null)
        {
            orders = orders.Where(order => order.ManagerId == managerId);
        }

        var totals = await entries
            .Join(
                context.Tasks,
                entry => entry.TaskId,
                task => task.TaskId,
                (entry, task) => new { Entry = entry, Task = task })
            .Join(
                orders,
                item => item.Task.OrderId,
                order => (OrderId?)order.Id,
                (item, order) => new
                {
                    item.Entry,
                    OrderId = order.Id,
                    OrderName = order.Name
                })
            .GroupBy(item => new { item.OrderId, item.OrderName })
            .Select(group => new
            {
                group.Key.OrderId,
                group.Key.OrderName,
                TotalHours = group.Sum(item => item.Entry.Hours)
            })
            .OrderByDescending(item => item.TotalHours)
            .ThenBy(item => item.OrderId)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return totals
            .Select(item => new TopOrdersReportDto(
                item.OrderId.Value,
                item.OrderName.Value,
                item.TotalHours))
            .ToArray();
    }
}
