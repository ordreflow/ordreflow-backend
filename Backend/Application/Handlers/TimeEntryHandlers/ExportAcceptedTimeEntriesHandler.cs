using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class ExportAcceptedTimeEntriesHandler(
    ITimeEntryRepository timeEntryRepository,
    IUserRepository userRepository,
    ITaskRepository taskRepository,
    IOrderRepository orderRepository) : IQueryHandler<
        ExportAcceptedTimeEntriesQuery,
        Result<IReadOnlyList<TimeEntryExportRow>>>
{
    public async Task<Result<IReadOnlyList<TimeEntryExportRow>>> HandleAsync(
        ExportAcceptedTimeEntriesQuery query)
    {
        if (query.ToDateExclusive <= query.FromDate)
            return Result<IReadOnlyList<TimeEntryExportRow>>.Failure(
                new Error("InvalidDateRange", "The export end must be after the export start."));

        var requesterIdResult = UserId.Create(query.RequestedBy);

        if (requesterIdResult.IsFailure)
            return Result<IReadOnlyList<TimeEntryExportRow>>.Failure(requesterIdResult.Errors.ToArray());

        var requester = await userRepository.GetAsync(requesterIdResult.Value);
        if (requester is null || !requester.CanReviewTimeEntries)
            return Result<IReadOnlyList<TimeEntryExportRow>>.Failure(
                new Error("ExportForbidden", "The user is not authorized to export time entries."));

        var entries = (await timeEntryRepository.GetByDateRangeAsync(
                query.FromDate,
                query.ToDateExclusive))
            .Where(entry => entry.Status is TimeEntryStatus.Accepted or TimeEntryStatus.Finalized)
            .ToArray();

        var employees = new Dictionary<UserId, Domain.Aggregate.User?>();
        foreach (var employeeId in entries.Select(entry => entry.EmployeeId).Distinct())
            employees[employeeId] = await userRepository.GetAsync(employeeId);

        // Admins export everyone; a manager only exports their own employees, as in the review list.
        if (requester.Role != UserRole.Admin)
            entries = entries
                .Where(entry => employees[entry.EmployeeId]?.ManagerId == requester.UserId)
                .ToArray();

        var tasks = new Dictionary<TaskId, Domain.Entities.Task?>();
        foreach (var taskId in entries.Select(entry => entry.TaskId).Distinct())
            tasks[taskId] = await taskRepository.GetAsync(taskId);

        var orderNames = new Dictionary<OrderId, string>();
        foreach (var orderId in tasks.Values.Select(task => task?.OrderId).OfType<OrderId>().Distinct())
            orderNames[orderId] = (await orderRepository.GetAsync(orderId))?.Name.Value ?? string.Empty;

        var rows = entries
            .Select(entry =>
            {
                var task = tasks[entry.TaskId];
                return new TimeEntryExportRow(
                    entry.Id.Value,
                    entry.EmployeeId.Value,
                    employees[entry.EmployeeId]?.Name.Value ?? string.Empty,
                    entry.TaskId.Value,
                    task?.Title ?? string.Empty,
                    task?.OrderId is { } orderId ? orderNames[orderId] : string.Empty,
                    entry.Date,
                    entry.Hours,
                    entry.Comment,
                    entry.Status.ToString());
            })
            .OrderBy(row => row.Date)
            .ThenBy(row => row.EmployeeName)
            .ToArray();

        return Result<IReadOnlyList<TimeEntryExportRow>>.Success(rows);
    }
}
