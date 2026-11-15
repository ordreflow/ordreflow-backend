using System.Data;
using Application.Exports;
using Application.Interfaces;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Persistence.Repositories;

public sealed class ExportRepository(AppDbContext context) : IExportRepository
{
    public Task<bool> EmployeeExistsAsync(EmployeeId employee, CancellationToken ct) =>
        context.Employees.AnyAsync(item => item.Id == employee, ct);

    public async Task<IReadOnlyList<ExportCandidate>> SelectApprovedAsync(DateOnly from, DateOnly to, EmployeeId? employee, CancellationToken ct)
    {
        // WorkDate is a converted value object. Filter its stored date in SQL, not .Value in LINQ.
        var latestApproved = context.TimeEntryAudits.FromSqlInterpolated($"""
            SELECT a.* FROM time_entry_audits a
            WHERE a.work_date >= {from} AND a.work_date <= {to} AND a.status = 'Approved'
              AND NOT EXISTS (SELECT 1 FROM time_entry_audits newer
                  WHERE newer.time_entry_id = a.time_entry_id AND newer.audit_time > a.audit_time)
            """).AsNoTracking();
        var selection = await (from audit in latestApproved
            join entry in context.TimeEntries.AsNoTracking() on audit.TimeEntryId equals entry.Id
            join owner in context.Employees.AsNoTracking() on entry.EmployeeId equals owner.Id
            join task in context.Tasks.AsNoTracking() on audit.TaskId equals task.Id
            join category in context.Catagories.AsNoTracking() on task.CatagoryId equals category.Id
            join department in context.Departments.AsNoTracking() on category.DepartmentId equals department.Id
            where employee == null || entry.EmployeeId == employee
            select new { entry.Id, AuditId = audit.Id, EmployeeName = owner.Name,
                TaskName = task.Name, CategoryName = category.Name, DepartmentName = department.Name }).ToListAsync(ct);

        var ids = selection.Select(item => item.Id).ToArray();
        var entries = await context.TimeEntries.AsNoTracking().Where(item => ids.Contains(item.Id))
            .Include(item => item.Audits).ToDictionaryAsync(item => item.Id, ct);
        return selection.Where(item => entries[item.Id].GetCurrentAudit()?.Id == item.AuditId)
            .Select(item => new ExportCandidate(entries[item.Id], item.EmployeeName.Value, item.TaskName.Value,
                item.CategoryName.Value, item.DepartmentName.Value))
            .OrderBy(item => item.Entry.CurrentWorkDate!.Value)
            .ThenBy(item => item.EmployeeName, StringComparer.Ordinal)
            .ThenBy(item => item.Entry.Id.Value).ToArray();
    }

    public Task<Export?> GetAsync(ExportId id, CancellationToken ct) => context.Exports.AsNoTracking()
        .Include(batch => batch.TimeEntries).SingleOrDefaultAsync(batch => batch.Id == id, ct);

    public async Task<IReadOnlyList<ExportSummaryDto>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        // Do not load the stored CSV payload just to display export history.
        var batches = await context.Exports.AsNoTracking().OrderByDescending(batch => batch.CreatedAt).ThenBy(batch => batch.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(batch => new { batch.Id, batch.ExportedBy, batch.CreatedAt, batch.DateFrom, batch.DateTo,
                batch.SelectedEmployeeId, EntryCount = batch.TimeEntries.Count, batch.TotalHours,
                batch.TimeEntriesClosedBy, batch.TimeEntriesClosedAtUtc }).ToArrayAsync(ct);
        return batches.Select(batch => new ExportSummaryDto(batch.Id.Value, batch.ExportedBy.Value, batch.CreatedAt,
            batch.DateFrom, batch.DateTo, batch.SelectedEmployeeId?.Value, batch.EntryCount, batch.TotalHours,
            batch.TimeEntriesClosedBy?.Value, batch.TimeEntriesClosedAtUtc)).ToArray();
    }

    public async Task<IReadOnlyList<ExportEmployeeDto>> EmployeesAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        var query = string.IsNullOrWhiteSpace(search) ? context.Employees.AsQueryable()
            : context.Employees.FromSqlInterpolated($"SELECT * FROM employees WHERE lower(name) LIKE lower({"%" + search + "%"})");
        var employees = await query.AsNoTracking().OrderBy(item => item.Name).ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return employees.Select(item => new ExportEmployeeDto(item.Id.Value, item.Name.Value, item.Status.ToString())).ToArray();
    }

    public async Task<Result<Export>> SaveNewAsync(Export batch, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var existing = await GetAsync(batch.Id, ct);
            if (existing is not null) return MatchExisting(existing, batch);
            var ids = batch.TimeEntries.Select(item => item.TimeEntryId).ToArray();
            var entries = await context.TimeEntries.AsNoTracking().Where(item => ids.Contains(item.Id))
                .Include(item => item.Audits).ToDictionaryAsync(item => item.Id, ct);
            if (entries.Count != ids.Length || batch.TimeEntries.Any(item =>
                !entries.TryGetValue(item.TimeEntryId, out var entry) ||
                entry.GetCurrentAudit() is not { Status: TimeEntryStatus.Approved } audit || audit.Id != item.TimeEntryAuditId))
                return Result<Export>.Failure(new Error("ExportSelectionConflict", "The approved selection changed; create the export again."));
            context.Exports.Add(batch);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Result<Export>.Success(batch);
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Dispose rolls back an active transaction and also handles PostgreSQL
            // having already aborted it (for example, a serialization failure at commit).
            await transaction.DisposeAsync();
            context.ChangeTracker.Clear();
            var existing = await GetAsync(batch.Id, ct);
            if (existing is null) throw;
            return MatchExisting(existing, batch);
        }
        catch (Exception error) when (IsConcurrencyConflict(error))
        {
            await transaction.DisposeAsync();
            context.ChangeTracker.Clear();
            return Result<Export>.Failure(new Error("ExportSelectionConflict", "The selection changed concurrently; retry with the same export ID."));
        }
    }

    public async Task<Result> CloseTimeEntriesAsync(ExportId id, EmployeeId actor, DateTime closedAtUtc, CancellationToken ct)
    {
        // Serializable isolation plus revision tokens protects both the batch and its entries.
        // Retrying reloads Closed entries when another overlapping batch won the race.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            try
            {
                var batch = await context.Exports.Include(item => item.TimeEntries).SingleOrDefaultAsync(item => item.Id == id, ct);
                if (batch is null) return Result.Failure(new Error("ExportNotFound", "The export was not found."));
                if (batch.TimeEntriesClosedAtUtc is not null) return Result.Success();
                var ids = batch.TimeEntries.Select(item => item.TimeEntryId).ToArray();
                var entries = await context.TimeEntries.Where(item => ids.Contains(item.Id)).OrderBy(item => item.Id)
                    .Include(item => item.Audits).ToArrayAsync(ct);
                var result = batch.CloseTimeEntries(entries, actor, closedAtUtc);
                if (result.IsFailure) return result;
                await context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return Result.Success();
            }
            catch (Exception error) when (IsConcurrencyConflict(error))
            {
                await transaction.DisposeAsync();
                context.ChangeTracker.Clear();
            }
        }
        return Result.Failure(new Error("ExportClosureConflict", "The entries changed concurrently; refresh and retry."));
    }

    private static Result<Export> MatchExisting(Export existing, Export requested) =>
        existing.MatchesRequest(requested.ExportedBy, requested.DateFrom, requested.DateTo, requested.SelectedEmployeeId)
            ? Result<Export>.Success(existing)
            : Result<Export>.Failure(new Error("ExportRequestConflict", "This export ID was used for a different request."));

    private static bool IsConcurrencyConflict(Exception error) => error is DbUpdateConcurrencyException ||
        error is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected } ||
        error is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected } };
}
