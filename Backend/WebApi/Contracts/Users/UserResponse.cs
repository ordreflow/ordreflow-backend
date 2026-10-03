namespace WebAPI.Contracts.Users;

public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    string Status,
    DateTime CreatedAt);
