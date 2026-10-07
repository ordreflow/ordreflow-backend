namespace WebAPI.Contracts.Orders;

public sealed record UpdateOrderRequest(
Guid Id,    
    string Name);
