using Application.Interfaces;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.ValueObjects;

namespace Application.Accounts;

/// <summary>
/// Backend checks for account administration (SEC-01). The API policies already require the permission;
/// handlers check again so a command can never run for an actor who has lost it.
/// </summary>
public sealed class AccountAccess(IAccountRepository accounts)
{
    public async Task<Result<Employee>> RequireAdministratorAsync(Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        return actor is not null && actor.HasPermission(SystemPermission.Employee) &&
               actor.HasPermission(SystemPermission.Administrator)
            ? Result<Employee>.Success(actor)
            : Result<Employee>.Failure(new Error("AdministrationForbidden", "Active administrator access is required."));
    }

    public async Task<Result<Employee>> RequireEmployeeAsync(Guid actorId, CancellationToken ct = default)
    {
        var actor = await GetActorAsync(actorId, ct);
        return actor is not null && actor.HasPermission(SystemPermission.Employee)
            ? Result<Employee>.Success(actor)
            : Result<Employee>.Failure(new Error("AccessForbidden", "Active employee access is required."));
    }

    private async Task<Employee?> GetActorAsync(Guid actorId, CancellationToken ct)
    {
        var id = EmployeeId.Create(actorId);
        return id.IsFailure ? null : await accounts.GetEmployeeAsync(id.Value, ct);
    }
}
