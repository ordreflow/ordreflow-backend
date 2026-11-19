namespace Application.Commands;

/// <summary>
/// Creates a category owned by the department. Internal-only categories are visible to its members only.
/// Reviewers must be active members or managers of the department and receive the category reviewer permission.
/// </summary>
public sealed record CreateCategoryCommand(
    Guid ActorId,
    Guid DepartmentId,
    string Name,
    bool InternalOnly,
    string? Description,
    IReadOnlyList<Guid>? ReviewerIds = null);

/// <summary>
/// Creates a project, ticket or task under a category (FR-08). Assignment-only work can only be selected by its
/// assignees; otherwise it is general work within the category's visibility.
/// </summary>
public sealed record CreateWorkItemCommand(
    Guid ActorId,
    Guid CategoryId,
    string Name,
    string? Description,
    string? Kind,
    bool AssignmentOnly,
    IReadOnlyList<WorkItemAssignmentInput>? Assignments = null);

public sealed record WorkItemAssignmentInput(Guid EmployeeId, decimal ExpectedHours, DateOnly ExpectedDeadline);

/// <summary>
/// Changes a category's name and description. It stays in its department. A non-null reviewer list replaces the
/// category's reviewers; null leaves them unchanged.
/// </summary>
public sealed record UpdateCategoryCommand(
    Guid ActorId,
    Guid CategoryId,
    string Name,
    string? Description,
    IReadOnlyList<Guid>? ReviewerIds = null);

/// <summary>
/// Changes a work item and replaces its assignments. Work stays in its category (SEC-08); assignees must belong to
/// the owning department.
/// </summary>
public sealed record UpdateWorkItemCommand(
    Guid ActorId,
    Guid WorkItemId,
    string Name,
    string? Description,
    string? Kind,
    bool AssignmentOnly,
    IReadOnlyList<WorkItemAssignmentInput>? Assignments);
