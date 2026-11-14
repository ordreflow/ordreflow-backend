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

        var reviewer = await employeeAccessRepository.GetWithAccessesAsync(EmployeeId.Create(query.ReviewerId).Value);
        if (reviewer is null || !reviewer.CanReviewTimeEntries())
            return Result<IReadOnlyList<TimeEntryDto>>.Failure(
                new Error("ReviewForbidden", "An active category reviewer permission is required."));

        // This query is the review queue; an optional status filter cannot broaden it.
        if (query.Status is { } status && status != TimeEntryStatus.ReadyForApproval)
            return Result<IReadOnlyList<TimeEntryDto>>.Success(Array.Empty<TimeEntryDto>());

        var catagoryIds = await reviewQueueRepository.GetAssignedCatagoryIdsAsync(reviewer.Id);
        if (catagoryIds.Count == 0)
            return Result<IReadOnlyList<TimeEntryDto>>.Success(Array.Empty<TimeEntryDto>());

        var taskIds = await reviewQueueRepository.GetTaskIdsAsync(catagoryIds);
        if (taskIds.Count == 0)
            return Result<IReadOnlyList<TimeEntryDto>>.Success(Array.Empty<TimeEntryDto>());

        var authorizedTaskIds = taskIds.ToHashSet();
        var entries = await reviewQueueRepository.GetCandidatesWithAuditsAsync(taskIds);
        var filtered = new List<TimeEntryDto>();

        foreach (var entry in entries)
        {
            var audit = entry.GetCurrentAudit();
            if (audit is null || !authorizedTaskIds.Contains(audit.TaskId) ||
                audit.Status != TimeEntryStatus.ReadyForApproval)
                continue;

            if (query.FromDate is { } from && audit.WorkDate.Value < DateOnly.FromDateTime(from))
                continue;

            if (query.ToDateExclusive is { } to && audit.WorkDate.Value >= DateOnly.FromDateTime(to))
                continue;

            filtered.Add(ToDto(entry, audit));
        }

        return Result<IReadOnlyList<TimeEntryDto>>.Success(filtered
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.Id)
            .ToArray());
    }

    private static TimeEntryDto ToDto(TimeEntry entry, TimeEntryAudit audit) => new(
        entry.Id.Value,
        audit.TaskId.Value,
        audit.WorkDate.Value.ToDateTime(TimeOnly.MinValue),
        audit.Duration.Value,
        audit.Description.Value,
        entry.EmployeeId.Value,
        audit.Status.ToString());
}
