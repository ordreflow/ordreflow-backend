using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetTimeEntryHandler(
    ITimeEntryRepository timeEntryRepository) : IQueryHandler<
        GetTimeEntryQuery,
        Result<TimeEntryDto>>
{
    public async Task<Result<TimeEntryDto>> HandleAsync(GetTimeEntryQuery query)
    {
        var employeeIdResult = EmployeeId.Create(query.ActorId);
        if (employeeIdResult.IsFailure)
            return Result<TimeEntryDto>.Failure(employeeIdResult.Errors.ToArray());
        var entryIdResult = TimeEntryId.Create(query.TimeEntryId);
        if (entryIdResult.IsFailure)
            return Result<TimeEntryDto>.Failure(entryIdResult.Errors.ToArray());

        var entry = await timeEntryRepository.GetWithHistoryAsync(entryIdResult.Value);
        if (entry is null)
            return Result<TimeEntryDto>.Failure(new Error("TimeEntryNotFound", "The time entry was not found."));

        if (entry.EmployeeId != employeeIdResult.Value)
            return Result<TimeEntryDto>.Failure(
                new Error("TimeEntryAccessForbidden", "You are not authorized to view this time entry."));

        var audit = entry.GetCurrentAudit();
        if (audit is null || audit.Status == TimeEntryStatus.Deleted)
            return Result<TimeEntryDto>.Failure(new Error("TimeEntryNotFound", "The time entry was not found."));

        return Result<TimeEntryDto>.Success(new TimeEntryDto(
            entry.Id.Value,
            audit.TaskId.Value,
            audit.WorkDate.Value.ToDateTime(TimeOnly.MinValue),
            (decimal)audit.Duration.Value,
            audit.Description.Value,
            entry.EmployeeId.Value,
            audit.Status.ToString(),
            entry.CurrentRejectionReason?.Value));
    }
}
