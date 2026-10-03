namespace WebAPI.Contracts.Users;

public sealed record CreateUserRequest(
    string Name,
    string Email,
    string Role);
