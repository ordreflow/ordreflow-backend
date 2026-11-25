using Application.Commands;
using Application.Exports;
using Application.Interfaces;
using Core.Tools.OperationResult;
using Domain.Services;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class CloseExportTimeEntriesHandler(ExportAccess access, IExportRepository exports,
    ExportClosureDomainService closure, TimeProvider clock) : ICommandHandler<CloseExportTimeEntriesCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(CloseExportTimeEntriesCommand command, CancellationToken ct = default)
    {
        var actor = await access.RequireAsync(command.RequestedBy, ct);
        if (actor.IsFailure) return Result<bool>.Failure(actor.Errors.ToArray());
        var id = ExportId.Create(command.ExportId);
        if (id.IsFailure) return Result<bool>.Failure(id.Errors.ToArray());
        var batch = await exports.GetForClosureAsync(id.Value, ct);
        if (batch is null) return Result<bool>.Failure(new Error("ExportNotFound", "The export was not found."));
        if (batch.TimeEntriesClosedAtUtc is not null) return Result<bool>.Success(true);
        var entries = await exports.GetEntriesForClosureAsync(batch.TimeEntries.Select(item => item.TimeEntryId).ToArray(), ct);
        var result = closure.Close(batch, entries, actor.Value, clock.GetUtcNow().UtcDateTime);
        return result.IsFailure ? Result<bool>.Failure(result.Errors.ToArray()) : Result<bool>.Success(true);
    }
}
