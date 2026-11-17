namespace Application.Dtos;

public sealed record DepartmentDto(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    bool IsManagedByMe,
    IReadOnlyList<DepartmentMemberDto> Members);

public sealed record DepartmentMemberDto(
    Guid UserId,
    string Name,
    string Email,
    string Role,
    string Status,
    string? JobTitle,
    bool IsManager);
