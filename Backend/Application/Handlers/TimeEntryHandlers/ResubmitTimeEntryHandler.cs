using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class ResubmitTimeEntryHandler(
    ITimeEntryRepository timeEntryRepository,
    IRegistrationPeriodModeRepository registrationPeriodModeRepository,
    TimeProvider clock,
    TimeZoneInfo companyTimeZone)
    : ICommandHandler<ResubmitTimeEntryCommand>
{
    public async Task<Result> HandleAsync(ResubmitTimeEntryCommand command)
    {
        var entryId = TimeEntryId.Create(command.TimeEntryId);
        var employeeId = EmployeeId.Create(command.EmployeeId);
        if (entryId.IsFailure || employeeId.IsFailure)
            return Result.Failure((entryId.IsFailure ? entryId.Errors : employeeId.Errors).ToArray());

        var entry = await timeEntryRepository.GetWithHistoryAsync(entryId.Value);
        if (entry is null)
            return Result.Failure(new Error("TimeEntryNotFound", "The time entry was not found."));

        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, companyTimeZone).DateTime);
        var permittedDates = await TimeEntryHandlerSupport.GetPermittedWorkDatesAsync(
            registrationPeriodModeRepository,
            today);
        if (permittedDates.IsFailure)
            return Result.Failure(permittedDates.Errors.ToArray());

        return entry.Resubmit(
            employeeId.Value,
            permittedDates.Value,
            now.UtcDateTime);
    }
}
