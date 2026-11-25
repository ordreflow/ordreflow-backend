using Application.Exports;
using Application.Interfaces;
using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

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

    public async Task AddAsync(Export batch, CancellationToken ct) => await context.Exports.AddAsync(batch, ct);

    public Task<Export?> GetForClosureAsync(ExportId id, CancellationToken ct) => context.Exports.AsTracking()
        .Include(item => item.TimeEntries).SingleOrDefaultAsync(item => item.Id == id, ct);

    public async Task<IReadOnlyList<TimeEntry>> GetEntriesForClosureAsync(IReadOnlyCollection<TimeEntryId> ids, CancellationToken ct) =>
        await context.TimeEntries.AsTracking().Where(item => ids.Contains(item.Id)).OrderBy(item => item.Id)
            .Include(item => item.Audits).ToArrayAsync(ct);
}
