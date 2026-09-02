using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.Interfaces;

namespace Application.Handlers;

public class CreateTimeEntryHandler : ICommandHandler<CreateTimeEntryCommand>
{
    private readonly ITimeEntryRepository _timeEntryRepository;

    public CreateTimeEntryHandler(
        ITimeEntryRepository timeEntryRepository)
    {
        _timeEntryRepository = timeEntryRepository;
    }

    public async Task<Result> HandleAsync(
        CreateTimeEntryCommand command)
    {
        if (command.WorkItemId <= 0)
        {
            return Result.Failure(
                new Error(
                    "INVALID_WORK_ITEM",
                    "Work item is required.",
                    "Validation"));
        }

        if (command.Date == default)
        {
            return Result.Failure(
                new Error(
                    "INVALID_DATE",
                    "Date is required.",
                    "Validation"));
        }

        if (command.Hours <= 0)
        {
            return Result.Failure(
                new Error(
                    "INVALID_HOURS",
                    "Hours must be greater than 0.",
                    "Validation"));
        }

        if (command.Hours > 24)
        {
            return Result.Failure(
                new Error(
                    "INVALID_HOURS",
                    "Hours cannot be greater than 24.",
                    "Validation"));
        }

        if (command.StartTime.HasValue &&
            command.EndTime.HasValue &&
            command.EndTime <= command.StartTime)
        {
            return Result.Failure(
                new Error(
                    "INVALID_TIME_RANGE",
                    "End time must be greater than start time.",
                    "Validation"));
        }

        var timeEntry = new TimeEntry(
            command.WorkItemId,
            command.Date,
            command.Hours,
            command.StartTime,
            command.EndTime,
            command.Comment);

        await _timeEntryRepository.CreateAsync(
            timeEntry);

        return Result.Success();
    }
}