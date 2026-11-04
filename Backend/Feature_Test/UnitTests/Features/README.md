# Functional requirement unit-test index (FR-01–FR-15)

Source: [M2 core employee MVP requirements (Draft)](https://github.com/ordreflow/ordreflow-docs/blob/main/Requirements/M2-core-employee-mvp-requirements.md).

Create an `FR-XX/` folder here only when requirement-specific unit tests exist; do not add 15 empty placeholder folders. Keep existing layer-specific handler and WebApi unit tests in their current locations rather than duplicating them here. HTTP, cross-feature process, dispatcher, and generic support tests remain in their existing suites.

Links below identify **related direct unit-test cases**, not proof of full requirement coverage. “None” means no direct unit-test file is indexed for that requirement here; it does not assert that the behavior is untested elsewhere.
These tests describe the current backend; future acceptance criteria may require different behavior.

| ID | Draft requirement (short label) | Related direct unit tests |
| --- | --- | --- |
| FR-01 | Register time on assigned order/task | [Registration scope](FR-01/TimeRegistrationScopeTests.cs); [entry ownership](FR-01/TimeRegistrationOwnershipTests.cs); [creation boundaries](FR-01/TimeEntryCreationBoundariesTests.cs); [closed-task registration](FR-01/ClosedTaskTimeRegistrationTests.cs); [create handler](../Application/Handlers/TimeEntries/CreateTimeEntryHandlerTests.cs) (task required; assigned means employee's manager matches order manager) |
| FR-02 | Manager views scoped time entries | [access](FR-02/ManagerTimeEntryAccessTests.cs); [date boundaries](FR-02/ManagerTimeEntryDateBoundariesTests.cs); [status filter](FR-02/ManagerTimeEntryStatusFilterTests.cs); [ordering and DTO projection](FR-02/ManagerTimeEntryProjectionTests.cs); [time-entry query handlers](../Application/Handlers/TimeEntries/TimeEntryQueryHandlerTests.cs) (unit tests use pre-scoped entries; actual employee.ManagerId scope filtering in the repository is not verified here) |
| FR-03 | Manager reviews/approves time entries | [accept](FR-03/AcceptTimeEntryTests.cs); [finalize](FR-03/FinalizeTimeEntryTests.cs); [command validation](FR-03/ReviewCommandValidationTests.cs) (invoice eligibility is not defined by these tests) |
| FR-04 | Employee views own week entries | [selection](FR-04/EmployeeWeekEntriesSelectionTests.cs); [projection](FR-04/EmployeeWeekEntriesProjectionTests.cs); [time-entry query handlers](../Application/Handlers/TimeEntries/TimeEntryQueryHandlerTests.cs) |
| FR-05 | Admin creates users | [creation](FR-05/AdminCreatesUserTests.cs); [rejections](FR-05/CreateUserRejectionTests.cs) |
| FR-06 | Pilot login | [login boundaries](FR-06/LoginBoundaryTests.cs); [login handler](../Application/Handlers/Auth/LoginHandlerTests.cs) |
| FR-07 | Export accepted entries | [validation and access](FR-07/ExportValidationAndAccessTests.cs); [missing references](FR-07/ExportMissingReferencesTests.cs); [export handler](../Application/Handlers/TimeEntries/ExportAcceptedTimeEntriesHandlerTests.cs); [CSV formatting](../WebApi/Formatting/TimeEntryExportCsvTests.cs) |
| FR-08 | Manager creates orders/tasks | [order creation](FR-08/ManagerOrderCreationTests.cs); [task creation](FR-08/ManagerTaskCreationTests.cs) |
| FR-09 | Employee views assigned orders/tasks | [orders](FR-09/AssignedOrderQueryTests.cs); [tasks](FR-09/AssignedTaskQueryTests.cs); [unassigned lists](FR-09/UnassignedEmployeeListTests.cs) (unit fakes supply scoped lists; repository filtering is not verified here) |
| FR-10 | Correct editable time entry | [time-entry edits](FR-10/TimeEntryEditTests.cs); [update lookups](FR-10/UpdateTimeEntryLookupTests.cs); [update orchestration](FR-10/UpdateTimeEntryOrchestrationTests.cs) |
| FR-11 | Top 10 orders by registered hours | [report handler validation/delegation](../Application/Handlers/Reports/GetTopOrdersHandlerTests.cs) (default filter includes Accepted and Finalized; repository ranking and D3.js visualization are not unit-tested here) |
| FR-12 | Returned reason, correction, resubmission | [return](FR-12/ReturnTimeEntryTests.cs); [resubmit](FR-12/ResubmitTimeEntryTests.cs); [validation](FR-12/ReturnAndResubmitValidationTests.cs); [FR-10 returned-entry cases](FR-10/TimeEntryEditTests.cs); [review-history handler](../Application/Handlers/TimeEntryReviews/GetTimeEntryReviewsHandlerTests.cs) |
| FR-13 | Admin assigns roles | [role changes](FR-13/AdminChangesRoleTests.cs); [rejections](FR-13/RoleChangeRejectionTests.cs) |
| FR-14 | Admin assigns employees to managers | [assignment](FR-14/AdminAssignsManagerTests.cs); [rejections](FR-14/AssignManagerRejectionTests.cs) |
| FR-15 | Employee views weekly total | [aggregation](FR-15/EmployeeWeeklyTotalAggregationTests.cs); [validation](FR-15/EmployeeWeeklyTotalValidationTests.cs) (unit fakes supply employee-scoped entries; repository ownership filtering is not verified here) |
