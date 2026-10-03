namespace WebAPI.Contracts.Users;

public sealed record ViewUsersResponse(
    IReadOnlyCollection<UserResponse> Items);
