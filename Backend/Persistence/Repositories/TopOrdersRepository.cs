using Application.Dtos;
using Application.Interfaces;
using Domain.Aggregate;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public sealed class TopOrdersRepository(AppDbContext context)
    : ITopOrdersRepository
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
            .Where(entry =>
                entry.Date >= fromDate &&
                entry.Date <= toDate);

        if (onlyAccepted)
        {
            entries = entries.Where(entry =>
                entry.Status == TimeEntryStatus.Accepted ||
                entry.Status == TimeEntryStatus.Finalized);
        }

        IQueryable<Order> orders = context.Orders.AsNoTracking();

        if (managerId is not null)
        {
            orders = orders.Where(order =>
                order.ManagerId == managerId);
        }

        // First calculate hours for each task within each order.
        var taskTotals = await entries
            .Join(
                context.Tasks.AsNoTracking(),
                entry => entry.TaskId,
                task => task.TaskId,
                (entry, task) => new
                {
                    Entry = entry,
                    Task = task
                })
            .Join(
                orders,
                item => item.Task.OrderId,
                order => (OrderId?)order.Id,
                (item, order) => new
                {
                    item.Entry,
                    item.Task,
                    Order = order
                })
            .GroupBy(item => new
            {
                item.Order.Id,
                item.Order.Name,
                item.Task.TaskId,
                item.Task.Title
            })
            .Select(group => new
            {
                OrderId = group.Key.Id,
                OrderName = group.Key.Name,
                TaskId = group.Key.TaskId,
                TaskName = group.Key.Title,
                TotalHours = group.Sum(item => item.Entry.Hours)
            })
            .ToListAsync(cancellationToken);

        // Then group the task totals by order in memory.
        // This keeps the EF query straightforward and avoids
        // complicated nested collection projections.
        var result = taskTotals
            .GroupBy(item => new
            {
                item.OrderId,
                item.OrderName
            })
            .Select(group => new
            {
                group.Key.OrderId,
                group.Key.OrderName,
                TotalHours = group.Sum(item => item.TotalHours),
                Tasks = group.Select(item => new TopOrderTaskDto(
                    item.TaskId.Value,
                    item.TaskName,
                    item.TotalHours))
                .OrderByDescending(task => task.TotalHours)
                .ToArray()
            })
           .OrderByDescending(order => order.TotalHours)
.ThenBy(order => order.OrderId.Value)
.Take(limit)
            .ToArray();

        return result.Select(order => new TopOrdersReportDto(
            order.OrderId.Value,
            order.OrderName.Value,
            order.TotalHours,
            order.Tasks)).ToArray();
    }
}