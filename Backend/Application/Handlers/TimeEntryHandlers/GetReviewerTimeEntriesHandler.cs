using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetReviewerTimeEntriesHandler(
    IEmployeeAccessRepository employeeAccessRepository,
    IReviewQueueRepository reviewQueueRepository) : IQueryHandler<
        GetReviewerTimeEntriesQuery,
        Result<IReadOnlyList<TimeEntryDto>>>
{
    public async Task<Result<IReadOnlyList<TimeEntryDto>>> HandleAsync(
        GetReviewerTimeEntriesQuery query)
    {
        var reviewerId = EmployeeId.Create(query.ReviewerId);
        if (reviewerId.IsFailure)
            return Result<IReadOnlyList<TimeEntryDto>>.Failure(
                reviewerId.Errors.ToArray());

        var reviewer = await employeeAccessRepository.GetWithAccessesAsync(reviewerId.Value);
        if (reviewer is null || !reviewer.CanReviewTimeEntries())
            return Result<IReadOnlyList<TimeEntryDto>>.Failure(
                new Error("ReviewForbidden", "An active category reviewer permission is required."));

        // The review list shows submitted entries and their outcomes; drafts and deleted entries stay private.
        if (query.Status is { } status && !IsReviewable(status))
            return Result<IReadOnlyList<TimeEntryDto>>.Success(Array.Empty<TimeEntryDto>());

        var catagoryIds = await reviewQueueRepository.GetAssignedCatagoryIdsAsync(reviewer.Id);
        if (catagoryIds.Count == 0)
            return Result<IReadOnlyList<TimeEntryDto>>.Success(Array.Empty<TimeEntryDto>());

        var taskIds = await reviewQueueRepository.GetTaskIdsAsync(catagoryIds);
        if (taskIds.Count == 0)
            return Result<IReadOnlyList<TimeEntryDto>>.Success(Array.Empty<TimeEntryDto>());

        var authorizedTaskIds = taskIds.ToHashSet();
        var entries = await reviewQueueRepository.GetCandidatesWithAuditsAsync(taskIds);
        var filtered = new List<(TimeEntry Entry, TimeEntryAudit Audit)>();

        foreach (var entry in entries)
        {
            var audit = entry.GetCurrentAudit();
            if (audit is null || !authorizedTaskIds.Contains(audit.TaskId) || !IsReviewable(audit.Status) ||
                (query.Status is { } wanted && audit.Status != wanted))
                continue;

            if (query.FromDate is { } from && audit.WorkDate.Value < DateOnly.FromDateTime(from))
                continue;

            if (query.ToDateExclusive is { } to && audit.WorkDate.Value >= DateOnly.FromDateTime(to))
                continue;

            filtered.Add((entry, audit));
        }

        if (filtered.Count == 0)
            return Result<IReadOnlyList<TimeEntryDto>>.Success(Array.Empty<TimeEntryDto>());

        var names = await reviewQueueRepository.GetDisplayNamesAsync(
            filtered.Select(item => item.Entry.EmployeeId).Distinct().ToArray(),
            filtered.Select(item => item.Audit.TaskId).Distinct().ToArray());

        return Result<IReadOnlyList<TimeEntryDto>>.Success(filtered
            .Select(item => ToDto(item.Entry, item.Audit, names))
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.Id)
            .ToArray());
    }

    private static bool IsReviewable(TimeEntryStatus status) => status is
        TimeEntryStatus.ReadyForApproval or TimeEntryStatus.Approved or TimeEntryStatus.Rejected or TimeEntryStatus.Closed;

    private static TimeEntryDto ToDto(TimeEntry entry, TimeEntryAudit audit, ReviewDisplayNames names)
    {
        names.Tasks.TryGetValue(audit.TaskId.Value, out var task);
        return new TimeEntryDto(
            entry.Id.Value,
            audit.TaskId.Value,
            audit.WorkDate.Value.ToDateTime(TimeOnly.MinValue),
            audit.Duration.Value,
            audit.Description.Value,
            entry.EmployeeId.Value,
            audit.Status.ToString(),
            entry.CurrentRejectionReason?.Value,
            names.Employees.GetValueOrDefault(entry.EmployeeId.Value),
            task?.TaskName,
            task?.CategoryName,
            task?.DepartmentName);
    }
}
