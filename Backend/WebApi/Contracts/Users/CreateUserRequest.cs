namespace WebAPI.Contracts.Users;

public sealed record CreateUserRequest(
    string Name,
    string Email,
    string Password,
    string Role,
    Guid? ManagerId = null);
