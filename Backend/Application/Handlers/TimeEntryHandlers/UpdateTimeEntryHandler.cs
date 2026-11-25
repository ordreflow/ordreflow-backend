using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class UpdateTimeEntryHandler(
    ITimeEntryRepository repository,
    IRegistrationPeriodModeRepository registrationPeriodModeRepository,
    TimeProvider clock,
    TimeZoneInfo companyTimeZone)
    : ICommandHandler<UpdateTimeEntryCommand>
{
    public async Task<Result> HandleAsync(UpdateTimeEntryCommand command)
    {
        var entryId = TimeEntryId.Create(command.TimeEntryId);
        var employeeId = EmployeeId.Create(command.EmployeeId);
        if (entryId.IsFailure)
            return Result.Failure(entryId.Errors.ToArray());
        if (employeeId.IsFailure)
            return Result.Failure(employeeId.Errors.ToArray());

        var entry = await repository.GetWithHistoryAsync(entryId.Value);
        if (entry is null)
            return Result.Failure(new Error("TimeEntryNotFound", "The time entry was not found."));

        var workDate = TimeEntryHandlerSupport.ToWorkDate(command.Date);
        var duration = Duration.Create(command.Hours);
        var description = TimeEntryDescription.Create(command.Comment);
        var validations = Result.Combine(workDate, duration, description);
        if (validations.IsFailure)
            return validations;

        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, companyTimeZone).DateTime);
        var permittedDates = await TimeEntryHandlerSupport.GetPermittedWorkDatesAsync(
            registrationPeriodModeRepository,
            today);
        if (permittedDates.IsFailure)
            return Result.Failure(permittedDates.Errors.ToArray());

        return entry.Change(
            employeeId.Value,
            workDate.Value,
            duration.Value,
            description.Value,
            permittedDates.Value,
            now.UtcDateTime);
    }
}
