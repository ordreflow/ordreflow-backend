namespace Application.Commands;

public sealed record CreateOrderCommand(
    Guid ManagerId,
    string Name);
