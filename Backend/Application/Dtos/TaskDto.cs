namespace Application.Dtos;

public sealed record TaskDto(
    Guid Id,
    Guid OrderId,
    string Title,
    string Description);
