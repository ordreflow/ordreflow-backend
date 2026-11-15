# UC-07: saved payroll exports

## Business rules

- An active employee with **Payroll or Administrator** system access can create,
  inspect, download, and close time entries in exports. A title, department-manager
  grant, or category-reviewer grant alone does not grant payroll access.
- Choose an inclusive work-date range and either all employees or one employee.
  Only entries whose **latest audit is Approved** are selected. Historical approved
  work from deactivated employees remains exportable.
- Creating an export saves a fixed set of time-entry IDs and approved audit IDs,
  its creator, date/employee filters, creation time, total hours, and original CSV.
  **Creation and every download leave entries Approved.**
- Re-download returns the original bytes, even after names change, more entries
  are approved, or the exported entries are Closed.
- The frontend's **Close time entries** button is a separate explicit write on a
  saved export. It closes that batch's fixed selection, never a fresh date query.
  Each entry receives a Closed audit with the closing actor and UTC time. The
  batch records who requested this action and when; no `Completed` time-entry
  status is introduced.
- Overlapping batches are allowed. An entry already Closed from the same approved
  version is skipped, retaining its original closing actor/time. Closing another
  batch closes its remaining Approved entries. Repeating closure is safe.
- A missing or changed approved version prevents the whole batch from closing.
  Closure uses one database transaction, revision concurrency tokens, serializable
  isolation, and bounded retries for concurrent closures. Approved and Closed
  entries remain locked against employee edits/deletes.

## API

All routes require bearer authentication and current Payroll-or-Administrator
permission. All such users can access this environment's export history.

### Create a batch

```http
POST /api/exports
Content-Type: application/json

{
  "exportId": "f39150c3-a02a-4488-8ffb-01f05f82818f",
  "fromDate": "2026-11-01",
  "toDate": "2026-11-30",
  "employeeId": null
}
```

The frontend generates one UUID for a create operation and retains it for retry.
Use a new UUID for an intentional new export, including an overlapping one.
Omit `employeeId` or use null for all employees. A supplied employee must exist;
deactivated employees are valid selections. Same-day ranges are valid.

Success: **201 Created**, a Location header, and:

```json
{
  "export": {
    "id": "f39150c3-a02a-4488-8ffb-01f05f82818f",
    "exportedBy": "00000000-0000-0000-0000-000000000001",
    "createdAtUtc": "2026-12-01T12:00:00Z",
    "fromDate": "2026-11-01",
    "toDate": "2026-11-30",
    "employeeId": null,
    "entryCount": 1,
    "totalHours": 7.5,
    "timeEntriesClosedBy": null,
    "timeEntriesClosedAtUtc": null
  },
  "entries": [
    {
      "timeEntryId": "00000000-0000-0000-0000-000000000002",
      "timeEntryAuditId": "00000000-0000-0000-0000-000000000003"
    }
  ],
  "downloadUrl": "/api/exports/f39150c3-a02a-4488-8ffb-01f05f82818f/download"
}
```

Retrying the same export ID with the same creator/filters returns the saved batch,
including after its entries are Closed. Reusing it with different filters or a
different creator returns **409 ExportRequestConflict**. If concurrent creation
returns a conflict, retry with the **same** export ID.

### History, details, employee selector, and downloads

| Request | Response |
| --- | --- |
| `GET /api/exports?page=1&pageSize=25` | `{ items: [summary], page, pageSize }`, newest first |
| `GET /api/exports/{id}` | Same details shape as creation, including saved entry/audit identifiers |
| `GET /api/exports/{id}/download` | Original `text/csv; charset=utf-8` file |
| `GET /api/exports/employees?search=Anna&page=1&pageSize=25` | `{ items: [{ id, name, status }], page, pageSize }`, including deactivated employees |

Page sizes are 1–100. The detail response identifies the exact exported versions;
the downloaded file contains their original payroll rows. A batch with a non-null
`timeEntriesClosedAtUtc` has already had the explicit close action applied. A
pending batch may contain some Closed entries due to overlapping batches.

The legacy `GET /api/time_entries/export` has been removed: a GET does not create
a saved export. Use POST followed by the returned download URL.

### Close a saved batch's time entries

```http
POST /api/exports/{id}/close-time-entries
```

No body. Success and repeat success: **204 No Content**. Refresh batch details
afterwards. This route never generates a new CSV or changes the saved file.

### Expected failures

- **400**: invalid/missing dates or identifiers, invalid pagination,
  `NoApprovedEntries` (no empty batch is stored).
- **401**: not authenticated.
- **403 ExportForbidden**: missing active payroll/administrator access.
- **404 EmployeeNotFound / ExportNotFound**: unknown employee or batch.
- **409 ExportRequestConflict / ExportSelectionConflict / ExportClosureConflict**:
  reused operation ID or a concurrent/inconsistent selection. Failed closure
  changes no entries. Conflict errors are arrays of `{ code, message }`.

## CSV

Columns: `Date,Employee,Department,Category,Task,Hours,Comment,Status,EmployeeId,TaskId,TimeEntryId`.
The department is the category's cost-bearing department. CSV uses UTF-8 with BOM,
ISO date-only values, decimal hours, invariant numbers, CRLF rows, quoted special
characters, and spreadsheet-formula protection for text. Rows are sorted by date,
employee name (ordinal), and entry ID. The status in the saved file is Approved,
including when downloaded after closure.

## Persistence and verification

Creation and closure use result-returning command handlers through
`UnitOfWorkCommandDispatcherDecorator` and `ICommandTransaction`. Repositories
load/stage aggregates; `ExportClosureDomainService` coordinates the cross-aggregate
rule. `CommandTransaction` owns the one serializable commit and bounded retries,
reloading state by rerunning the handler after a conflict. Read query handlers
perform no writes. Download URLs are constructed in WebApi, not Domain/Application.

EF mappings include stored CSV bytes, filter metadata, totals, batch revision,
closure actor/time, and a restricted foreign key from each export link to its
approved audit. Membership is unique **within** a batch, not globally by entry.

The repository's committed migration snapshot is still the M2 schema. These
changes must be included in the coordinated M3 migration with employees,
categories, time-entry audits, and the other new tables before deployment. Do not
apply an export-only migration against that M2 snapshot or use `EnsureCreated`
as a production migration strategy. Once the remaining projects compile, generate
and review the full M3 migration using the commands in the root README, including
how existing records are migrated.

Regression coverage is in `Backend/WebApiTests/IntegrationTest/PayrollExport*.cs`:
domain/service rules, auth grants, SQLite persistence, latest-version filtering,
inclusive dates, saved-file stability, overlaps, and explicit closure. Tests are
prepared for the integration pass; PostgreSQL concurrency and migration behavior
must also be exercised when the project is runnable. HTTP-flow coverage is in
`Backend/Feature_Test/TimeEntries/PayrollExportsHttpTests.cs`.
