using Domain.Aggregate;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Repositories;

public sealed class RegistrationPeriodModeRepository(AppDbContext context)
    : IRegistrationPeriodModeRepository
{
    public Task<RegistrationPeriodMode?> GetCurrentAsync(
        CancellationToken cancellationToken = default) =>
        context.RegistrationPeriodModes
            .AsNoTracking()
            .OrderByDescending(mode => mode.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);
}
