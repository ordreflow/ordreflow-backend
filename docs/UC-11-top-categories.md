# UC-11: top ten categories with task breakdowns

## Business rules

- An active Employee with **DepartmentManager or Administrator** system access can
  request the report. A displayed manager title, Payroll access, or category-reviewer
  access alone does not grant report access.
- Department managers see work under categories **owned by departments they currently
  manage**. Administrators see this environment's company-wide totals. Administrator
  access takes precedence if a user has both grants.
- Employee department membership does not determine report scope. Hours from someone
  in another department count toward the department that owns the selected category.
- Each entry contributes its **latest audit only**. Find the latest audit across all
  history before applying dates and scope. Draft, ReadyForApproval, Rejected, Approved,
  and Closed all count; Deleted does not.
- Filter by inclusive date-only **work dates**, not audit timestamps. A single-day
  range is valid. There is no conversion to UTC or exclusive-end `AddDays(1)`.
- Sum decimal hours per task and category, scope before ranking, and return at most
  ten categories. Each includes all its tasks with qualifying hours, not only its
  largest task. Categories/tasks with no qualifying entries are absent.
- Historical work still counts after an employee is deactivated or a category/task
  is closed. Explicit payroll closure does not remove hours from this report.
- Categories sort by hours descending, then category ID ascending. Tasks sort by
  hours descending, then task ID ascending. Equal display names do not merge IDs.
- No manager assignments or no matching work returns an empty report, never a
  company-wide fallback. Multiple manager/membership links do not multiply hours.

## API

```http
GET /api/reports/top-categories?fromDate=2026-11-01&toDate=2026-11-30
Authorization: Bearer <token>
```

Both dates are required. The backend derives the actor and department scope from
authentication and current grants. No client-supplied actor, department, limit,
or status filter is supported. The maximum is always ten categories and statuses
are always all current states except Deleted.

Success: **200 OK**:

```json
{
  "items": [
    {
      "categoryId": "00000000-0000-0000-0000-000000000001",
      "categoryName": "Customer projects",
      "departmentId": "00000000-0000-0000-0000-000000000002",
      "departmentName": "Installation",
      "totalHours": 125.5,
      "tasks": [
        {
          "taskId": "00000000-0000-0000-0000-000000000003",
          "taskName": "Site installation",
          "totalHours": 90
        },
        {
          "taskId": "00000000-0000-0000-0000-000000000004",
          "taskName": "Preparation",
          "totalHours": 35.5
        }
      ]
    }
  ]
}
```

The category total equals the sum of its task totals. An empty result is
`{ "items": [] }`. No employee identities, entry IDs, comments, or review reasons
are returned.

Expected failures:

- **400** for missing/malformed/reversed dates. Handler range errors use
  `[{ "code": "InvalidDateRange", "message": "Select a valid inclusive work-date range." }]`.
  MVC model-binding errors use the existing structured validation response.
- **401** for unauthenticated/invalid-token requests.
- **403** for inactive actors or missing current report grants. Handler permission
  errors use `ReportForbidden`; middleware can reject before dispatch.

The legacy `GET /api/reports/top-orders` route is removed. Its Order DTOs,
`OnlyAccepted`, and variable limit do not apply to this M3 report.

## Architecture and database behavior

```text
GetTopCategories endpoint
    → IQueryDispatcher
    → GetTopCategoriesHandler
    → ITopCategoriesQuery / Persistence.Queries.TopCategoriesQuery
    → category/task DTOs
```

Employee's report-eligibility predicate owns the fixed-grant rule. The handler
reloads the actor and decides company-wide versus manager identity scope. This is
a read projection: it does not need a domain service or a full Department graph.

Persistence performs latest-version selection, department filtering, task/category
aggregation, top-ten ranking, and row limiting in one parameterized SQL statement.
Converted WorkDate and Duration values are queried through their stored date and
numeric columns. Manager scope uses `EXISTS` against `department_managers`, avoiding
duplicate rows from link-table joins. PostgreSQL numeric sums return decimal hours.
Only task totals for the winning categories are materialized; no aggregates or
complete entry histories are loaded or mutated.

No UC-11-specific schema migration is required. The query depends on the coordinated
M3 schema (departments, categories, tasks, and time-entry audits), which must be
migrated before the API can run against it.

## Frontend integration

Use `categoryId` and `taskId` as D3 keys; names are display labels and may repeat.
Use `items` for category bars and each item's `tasks` for its breakdown, with the
department label available for context. Show loading, empty, and error states.
The backend returns chart-ready data; D3 rendering is a separate frontend step.

## Verification

Prepared regression coverage in `Backend/WebApiTests/IntegrationTest/`:

- `TopCategoriesHandlerTests.cs`: authorization, actor-derived scope, date validation.
- `TopCategoriesQueryTests.cs`: current-audit selection, date/status rules, exact
  totals, top-ten/ties, manager links, inactive owners, closed work, and empty results.
- `TopCategoriesHttpTests.cs`: authentication, policy grants, response shape,
  spoofed query parameters, inclusive dates, and empty/error responses.

These tests are compiled during this code-readiness pass but are not executed.
Database execution, PostgreSQL query behavior/performance, and the D3 integration
must be verified when the remaining M3 project is runnable.
