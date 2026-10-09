namespace Application.Dtos;

public sealed record UserDto(
    Guid Id,
    string Name,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAt);
