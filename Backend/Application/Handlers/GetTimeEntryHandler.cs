
    using Application.Commands;
    using Core.Tools.OperationResult;
    using Domain.Entities;
    using Domain.Interfaces;
    namespace Application.Handlers;


public class GetTimeEntryHandler : ICommandHandler<GetTimeEntriesCommand>
{
    private readonly ITimeEntryRepository _timeEntryRepository;

    public GetTimeEntryHandler(
        ITimeEntryRepository timeEntryRepository)
    {
        _timeEntryRepository = timeEntryRepository;
    }

    public async Task<Result> HandleAsync(
        GetTimeEntriesCommand command)
    {
        if (command.StartDate == default)
        {
            return Result.Failure(
                new Error(
                    "INVALID_START_DATE",
                    "Start date is required.",
                    "Validation"));
        }

        if (command.EndDate == default)
        {
            return Result.Failure(
                new Error(
                    "INVALID_END_DATE",
                    "End date is required.",
                    "Validation"));
        }

        if (command.EndDate < command.StartDate)
        {
            return Result.Failure(
                new Error(
                    "INVALID_DATE_RANGE",
                    "End date cannot be earlier than start date.",
                    "Validation"));
        }

        var timeEntries =
            await _timeEntryRepository.GetByDateRangeAsync(
                command.StartDate,
                command.EndDate);

           return Result<IEnumerable<TimeEntry>>.Success(timeEntries);

    }
}