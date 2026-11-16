using Application.Dtos;
using Application.Interfaces;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Queries;

public sealed class TopCategoriesQuery(AppDbContext context) : ITopCategoriesQuery
{
    public async Task<IReadOnlyList<TopCategoryReportDto>> GetAsync(EmployeeId? managerId,
        DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var allDepartments = managerId is null;
        var manager = managerId?.Value ?? Guid.Empty;
        // Unmapped read rows keep converted WorkDate/Duration objects out of SQL
        // expression translation. PostgreSQL sums the stored numeric hours exactly.
        // The newest-audit test deliberately inspects ALL history, not just this period.
        var rows = await context.Database.SqlQuery<ReportTaskRow>($"""
            WITH current_entries AS (
                SELECT a.task_id, a.duration
                FROM time_entry_audits a
                WHERE a.work_date >= {fromDate} AND a.work_date <= {toDate}
                  AND a.status <> 'Deleted'
                  AND NOT EXISTS (
                      SELECT 1 FROM time_entry_audits newer
                      WHERE newer.time_entry_id = a.time_entry_id AND newer.audit_time > a.audit_time
                  )
            ), task_totals AS (
                SELECT c.id AS "CategoryId", c.name AS "CategoryName",
                       d.id AS "DepartmentId", d.name AS "DepartmentName",
                       t.id AS "TaskId", t.name AS "TaskName", SUM(a.duration) AS "TotalHours"
                FROM current_entries a
                JOIN tasks t ON t.id = a.task_id
                JOIN catagories c ON c.id = t.catagory_id
                JOIN departments d ON d.id = c.department_id
                WHERE ({allDepartments} OR EXISTS (
                    SELECT 1 FROM department_managers m
                    WHERE m.department_id = d.id AND m.manager_id = {manager}
                ))
                GROUP BY c.id, c.name, d.id, d.name, t.id, t.name
            ), category_totals AS (
                SELECT "CategoryId", SUM("TotalHours") AS "CategoryHours"
                FROM task_totals GROUP BY "CategoryId"
            ), top_categories AS (
                SELECT "CategoryId", "CategoryHours" FROM category_totals
                ORDER BY "CategoryHours" DESC, "CategoryId" ASC LIMIT 10
            )
            SELECT t."CategoryId", t."CategoryName", t."DepartmentId", t."DepartmentName",
                   t."TaskId", t."TaskName", t."TotalHours", top."CategoryHours"
            FROM task_totals t
            JOIN top_categories top ON top."CategoryId" = t."CategoryId"
            ORDER BY top."CategoryHours" DESC, t."CategoryId" ASC, t."TotalHours" DESC, t."TaskId" ASC
            """).ToArrayAsync(cancellationToken);

        // SQL already scopes, aggregates, ranks, and limits. Only shape the returned
        // top-ten task rows into chart DTOs here; no entries or aggregates are loaded.
        return rows.GroupBy(row => row.CategoryId).Select(group =>
        {
            var category = group.First();
            return new TopCategoryReportDto(category.CategoryId, category.CategoryName,
                category.DepartmentId, category.DepartmentName, category.CategoryHours,
                group.Select(task => new TopCategoryTaskDto(task.TaskId, task.TaskName, task.TotalHours))
                    .OrderByDescending(task => task.TotalHours).ThenBy(task => task.TaskId).ToArray());
        }).OrderByDescending(category => category.TotalHours).ThenBy(category => category.CategoryId).ToArray();
    }

    private sealed class ReportTaskRow
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public Guid TaskId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public decimal CategoryHours { get; set; }
    }
}
