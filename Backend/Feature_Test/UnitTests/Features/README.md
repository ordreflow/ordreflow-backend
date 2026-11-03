# Functional requirement unit-test index (FR-01–FR-15)

Source: [M2 core employee MVP requirements (Draft)](https://github.com/ordreflow/ordreflow-docs/blob/main/Requirements/M2-core-employee-mvp-requirements.md).

Create an `FR-XX/` folder here only when requirement-specific unit tests exist; do not add 15 empty placeholder folders. Keep existing layer-specific handler and WebApi unit tests in their current locations rather than duplicating them here. HTTP, cross-feature process, dispatcher, and generic support tests remain in their existing suites.

Links below identify **related direct unit-test cases**, not proof of full requirement coverage. “None” means no direct unit-test file is indexed for that requirement here; it does not assert that the behavior is untested elsewhere.

| ID | Draft requirement (short label) | Related direct unit tests |
| --- | --- | --- |
| FR-01 | Register time on assigned order/task | [Registration scope](FR-01/TimeRegistrationScopeTests.cs); [entry ownership](FR-01/TimeRegistrationOwnershipTests.cs); [creation boundaries](FR-01/TimeEntryCreationBoundariesTests.cs); [closed-task registration](FR-01/ClosedTaskTimeRegistrationTests.cs); [create handler](../Application/Handlers/TimeEntries/CreateTimeEntryHandlerTests.cs) (task required; assigned means employee's manager matches order manager) |
| FR-02 | Manager views scoped time entries | [access](FR-02/ManagerTimeEntryAccessTests.cs); [date boundaries](FR-02/ManagerTimeEntryDateBoundariesTests.cs); [status filter](FR-02/ManagerTimeEntryStatusFilterTests.cs); [ordering and DTO projection](FR-02/ManagerTimeEntryProjectionTests.cs); [time-entry query handlers](../Application/Handlers/TimeEntries/TimeEntryQueryHandlerTests.cs) (unit tests use pre-scoped entries; actual employee.ManagerId scope filtering in the repository is not verified here) |
| FR-03 | Manager reviews/approves time entries | None directly for manager approval (the cross-feature process test is not a direct test) |
| FR-04 | Employee views own week entries | [time-entry query handlers](../Application/Handlers/TimeEntries/TimeEntryQueryHandlerTests.cs) |
| FR-05 | Admin creates users | None |
| FR-06 | Pilot login | [login handler](../Application/Handlers/Auth/LoginHandlerTests.cs) |
| FR-07 | Export accepted entries | [export handler](../Application/Handlers/TimeEntries/ExportAcceptedTimeEntriesHandlerTests.cs); [CSV formatting](../WebApi/Formatting/TimeEntryExportCsvTests.cs) |
| FR-08 | Manager creates orders/tasks | None |
| FR-09 | Employee views assigned orders/tasks | None |
| FR-10 | Correct editable time entry | [time-entry edits](FR-10/TimeEntryEditTests.cs) |
| FR-11 | Top 10 orders by registered hours | [top-orders handler](../Application/Handlers/Reports/GetTopOrdersHandlerTests.cs) |
| FR-12 | Returned reason, correction, resubmission | [FR-10 returned-entry cases](FR-10/TimeEntryEditTests.cs); [review-history handler](../Application/Handlers/TimeEntryReviews/GetTimeEntryReviewsHandlerTests.cs); [review response mapping](../WebApi/Mapping/TimeEntryReviewToResponseMapperTests.cs) |
| FR-13 | Admin assigns roles | None |
| FR-14 | Admin assigns employees to managers | None |
| FR-15 | Employee views weekly total | None |
