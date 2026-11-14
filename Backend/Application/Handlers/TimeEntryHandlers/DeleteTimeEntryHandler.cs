using Application.Commands;
using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class DeleteTimeEntryHandler(
    ITimeEntryRepository timeEntryRepository,
    IRegistrationPeriodModeRepository registrationPeriodModeRepository)
    : ICommandHandler<DeleteTimeEntryCommand>
{
    public async Task<Result> HandleAsync(DeleteTimeEntryCommand command)
    {
        var entryIdResult = TimeEntryId.Create(command.TimeEntryId);

        if (entryIdResult.IsFailure)
            return Result.Failure(entryIdResult.Errors.ToArray());

        var employeeIdResult = EmployeeId.Create(command.EmployeeId);

        if (employeeIdResult.IsFailure)
            return Result.Failure(employeeIdResult.Errors.ToArray());

        var entry = await timeEntryRepository.GetWithHistoryAsync(entryIdResult.Value);

        if (entry is null)
            return Result.Failure(new Error("TimeEntryNotFound", "The time entry was not found."));

        var permittedDates = await TimeEntryHandlerSupport.GetPermittedWorkDatesAsync(
            registrationPeriodModeRepository,
            DateOnly.FromDateTime(DateTime.UtcNow));
        if (permittedDates.IsFailure)
            return Result.Failure(permittedDates.Errors.ToArray());

        return entry.Delete(
            employeeIdResult.Value,
            permittedDates.Value,
            DateTime.UtcNow);
    }
}
