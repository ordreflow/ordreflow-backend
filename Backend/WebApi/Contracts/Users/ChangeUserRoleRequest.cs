namespace WebAPI.Contracts.Users;

public sealed record ChangeUserRoleRequest(
    Guid UserId,
    string Role);
