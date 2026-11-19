namespace WebAPI.Contracts.Accounts;

public sealed record CreateCategoryRequest(string Name, bool InternalOnly = false, string? Description = null);

/// <summary>Kind is "Project", "Ticket" or "Task" (default). Assignment-only work is selectable by assignees only.</summary>
public sealed record CreateWorkItemRequest(
    string Name,
    string? Description = null,
    string? Kind = null,
    bool AssignmentOnly = false,
    IReadOnlyList<CreateWorkItemAssignmentRequest>? Assignments = null);

public sealed record CreateWorkItemAssignmentRequest(Guid EmployeeId, decimal ExpectedHours, DateOnly ExpectedDeadline);

public sealed record UpdateCategoryRequest(string Name, string? Description = null);

/// <summary>Replaces the work item's details and its assignments.</summary>
public sealed record UpdateWorkItemRequest(
    string Name,
    string? Description = null,
    string? Kind = null,
    bool AssignmentOnly = false,
    IReadOnlyList<CreateWorkItemAssignmentRequest>? Assignments = null);
