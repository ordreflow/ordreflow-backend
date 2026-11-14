using Domain.Aggregate;

namespace Domain.Interfaces;

public interface IRegistrationPeriodModeRepository
{
    Task<RegistrationPeriodMode?> GetCurrentAsync(CancellationToken cancellationToken = default);
}
