using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

internal static class TimeEntryHandlerSupport
{
    internal static async Task<Result<WorkDatePeriod>> GetPermittedWorkDatesAsync(
        IRegistrationPeriodModeRepository repository,
        DateOnly today)
    {
        var mode = await repository.GetCurrentAsync();
        return mode is null
            ? Result<WorkDatePeriod>.Failure(
                new Error("RegistrationPeriodNotConfigured", "The registration period has not been configured."))
            : mode.GetPermittedWorkDates(today);
    }

    internal static Result<WorkDate> ToWorkDate(DateTime value) =>
        value == default
            ? Result<WorkDate>.Failure(new Error("WorkDateRequired", "A work date is required."))
            : WorkDate.Create(DateOnly.FromDateTime(value));
}
