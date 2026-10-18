namespace Application.Dtos;

public sealed record LoginDto(
    string AccessToken,
    DateTime ExpiresAt);
