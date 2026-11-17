# Functional requirement test index (FR-01–FR-15)

Source: [M3 requirements](https://github.com/ordreflow/ordreflow-docs/blob/main/Requirements/M3-ordreflow-requirements.md).

The M2 unit tests that were written against the old `User`/`Order` model have been removed together with that model. New tests for the M3 domain live mainly in `WebApiTests/IntegrationTest`. Create an `FR-XX/` folder here only when requirement-specific unit tests exist.

Links below identify **related tests**, not proof of full requirement coverage. "Not refactored yet" means the use case has not been moved to the M3 domain yet.

| ID | Requirement (short label) | Related tests |
| --- | --- | --- |
| FR-01 | Register time on accessible work | [handler](../../../WebApiTests/IntegrationTest/CreateTimeEntryHandlerTests.cs); [access rules](../../../WebApiTests/IntegrationTest/TimeRegistrationAccessDomainTests.cs); [persistence](../../../WebApiTests/IntegrationTest/TimeRegistrationRepositoryTests.cs); [HTTP flow](../../../WebApiTests/IntegrationTest/TimeRegistrationHttpTests.cs); [registration periods](../../../WebApiTests/IntegrationTest/RegistrationPeriodModeTests.cs) |
| FR-02 | Category reviewer views review queue | [handler](../../../WebApiTests/IntegrationTest/ReviewQueueHandlerTests.cs); [query](../../../WebApiTests/IntegrationTest/ReviewQueueRepositoryTests.cs); [HTTP](../../../WebApiTests/IntegrationTest/ReviewerTimeEntriesHttpTests.cs) |
| FR-03 | Approve or reject entries | [domain](../../../WebApiTests/IntegrationTest/ReviewDecisionDomainTests.cs); [domain service](../../../WebApiTests/IntegrationTest/ReviewDomainServiceTests.cs); [handler](../../../WebApiTests/IntegrationTest/ReviewDecisionHandlerTests.cs); [persistence](../../../WebApiTests/IntegrationTest/ReviewDecisionRepositoryTests.cs); [commit](../../../WebApiTests/IntegrationTest/ReviewDecisionCommitTests.cs); [HTTP](../../../WebApiTests/IntegrationTest/ReviewerDecisionHttpTests.cs) |
| FR-04 | Employee views own week entries | [weekly entries handler](../../../WebApiTests/IntegrationTest/WeeklyTimeEntriesHandlerTests.cs) |
| FR-05 | Admin creates and deactivates accounts | [accounts over HTTP](../../../WebApiTests/IntegrationTest/AccountsAndAccessTests.cs) (creation, validation, deactivation, reactivation, self-lockout protection) |
| FR-06 | Local sign-in | [login handler](../../../WebApiTests/IntegrationTest/LoginPermissionTests.cs); [HTTP](../../../WebApiTests/IntegrationTest/LoginAndRoleAccessTests.cs) |
| FR-07 | Saved Approved-entry payroll batches; explicit closure | [domain, service, and CSV rules](../../../WebApiTests/IntegrationTest/PayrollExportTests.cs); [authorization](../../../WebApiTests/IntegrationTest/PayrollExportAuthorizationTests.cs); [persistence](../../../WebApiTests/IntegrationTest/PayrollExportRepositoryTests.cs); [HTTP flow](../../TimeEntries/PayrollExportsHttpTests.cs) |
| FR-08 | Department manager creates work under permitted categories | [planning over HTTP](../../../WebApiTests/IntegrationTest/WorkPlanningHttpTests.cs); [planning rules and assignment-only work](../../../WebApiTests/IntegrationTest/WorkPlanningDomainTests.cs) |
| FR-09 | Employee views general and assigned work | [finding work over HTTP, Assigned to me, direct URL, search, performance](../../../WebApiTests/IntegrationTest/FindWorkHttpTests.cs); [selection rules match time registration](../../../WebApiTests/IntegrationTest/WorkSelectionRulesTests.cs) |
| FR-10 | Correct editable time entry | [corrections](../../../WebApiTests/IntegrationTest/TimeEntryCorrectionTests.cs) |
| FR-11 | Top ten categories with task totals | [handler and scope](../../../WebApiTests/IntegrationTest/TopCategoriesHandlerTests.cs); [query and ranking](../../../WebApiTests/IntegrationTest/TopCategoriesQueryTests.cs); [HTTP contract](../../../WebApiTests/IntegrationTest/TopCategoriesHttpTests.cs) (D3 rendering is a frontend integration step) |
| FR-12 | Understand a rejected entry | [corrections](../../../WebApiTests/IntegrationTest/TimeEntryCorrectionTests.cs) |
| FR-13 | System access independent of job title | [title and access over HTTP](../../../WebApiTests/IntegrationTest/AccountsAndAccessTests.cs); [domain rules](../../../WebApiTests/IntegrationTest/DepartmentStaffingDomainTests.cs) |
| FR-14 | Users and department managers in departments | [departments over HTTP](../../../WebApiTests/IntegrationTest/AccountsAndAccessTests.cs); [staffing rules](../../../WebApiTests/IntegrationTest/DepartmentStaffingDomainTests.cs) |
| FR-15 | Employee views weekly total | [weekly entries handler](../../../WebApiTests/IntegrationTest/WeeklyTimeEntriesHandlerTests.cs) |
