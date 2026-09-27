namespace WebAPI.Contracts.Orders;

public sealed record OrderResponse(
    Guid Id,
    string Name,
    string Status,
    DateTime CreatedAt,
    DateTime? ClosedAt);
