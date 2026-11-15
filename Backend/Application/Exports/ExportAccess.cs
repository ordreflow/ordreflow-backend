using Core.Tools.OperationResult;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Exports;

public sealed class ExportAccess(IEmployeeAccessRepository employees)
{
    public async Task<Result<EmployeeId>> RequireAsync(Guid actorId, CancellationToken ct)
    {
        var id = EmployeeId.Create(actorId);
        if (id.IsFailure) return Result<EmployeeId>.Failure(id.Errors.ToArray());
        var actor = await employees.GetWithAccessesAsync(id.Value, ct);
        return actor is not null && actor.CanExportTimeEntries() ? Result<EmployeeId>.Success(id.Value)
            : Result<EmployeeId>.Failure(new Error("ExportForbidden", "Active payroll or administrator access is required."));
    }
}
