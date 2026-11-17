namespace Application.Commands;

/// <summary>Creates a category owned by the department. Internal-only categories are visible to its members only.</summary>
public sealed record CreateCategoryCommand(
    Guid ActorId,
    Guid DepartmentId,
    string Name,
    bool InternalOnly,
    string? Description);

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
    bool AssignmentOnly);
