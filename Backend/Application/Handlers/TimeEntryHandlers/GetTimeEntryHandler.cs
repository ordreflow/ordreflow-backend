using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetTimeEntryHandler(
    ITimeEntryRepository timeEntryRepository,
    IUserRepository userRepository) : IQueryHandler<
        GetTimeEntryQuery,
        Result<TimeEntryDto>>
{
    public async Task<Result<TimeEntryDto>> HandleAsync(GetTimeEntryQuery query)
    {
        var actorIdResult = UserId.Create(query.ActorId);

        if (actorIdResult.IsFailure)
            return Result<TimeEntryDto>.Failure(actorIdResult.Errors.ToArray());

        var actor = await userRepository.GetAsync(actorIdResult.Value);

        if (actor is null)
            return Result<TimeEntryDto>.Failure(new Error("UserNotFound", "The current user could not be found."));

        var entryIdResult = TimeEntryId.Create(query.TimeEntryId);

        if (entryIdResult.IsFailure)
            return Result<TimeEntryDto>.Failure(entryIdResult.Errors.ToArray());

        var entry = await timeEntryRepository.GetAsync(entryIdResult.Value);

        if (entry is null)
            return Result<TimeEntryDto>.Failure(new Error("TimeEntryNotFound", "The time entry was not found."));

        var employee = await userRepository.GetAsync(entry.EmployeeId);

        if (!entry.CanView(actor.UserId, actor.Role, employee?.ManagerId))
        {
            return Result<TimeEntryDto>.Failure(
                new Error("TimeEntryAccessForbidden", "You are not authorized to view this time entry."));
        }

        return Result<TimeEntryDto>.Success(new TimeEntryDto(
            entry.Id.Value,
            entry.TaskId.Value,
            entry.Date,
            entry.Hours,
            entry.Comment));
    }
}
