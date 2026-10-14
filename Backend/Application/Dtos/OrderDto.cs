namespace Application.Dtos;

public sealed record OrderDto(
    Guid Id,
    string Name,
    string Status,
    DateTime CreatedAt,
    DateTime? ClosedAt);
