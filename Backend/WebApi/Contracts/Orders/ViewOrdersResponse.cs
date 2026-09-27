namespace WebAPI.Contracts.Orders;

public sealed record ViewOrdersResponse(
    IReadOnlyCollection<OrderResponse> Items);
