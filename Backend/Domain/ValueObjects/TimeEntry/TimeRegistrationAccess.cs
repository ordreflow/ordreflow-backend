namespace Domain.ValueObjects;

// Backend-resolved cross-aggregate facts for one employee and one work item.
public sealed record TimeRegistrationAccess(bool IsDepartmentMember, bool HasAssignments, bool IsAssigned);
