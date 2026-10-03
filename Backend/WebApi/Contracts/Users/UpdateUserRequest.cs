namespace WebAPI.Contracts.Users;

public sealed record UpdateUserRequest(
    string Name,
    string Email);
