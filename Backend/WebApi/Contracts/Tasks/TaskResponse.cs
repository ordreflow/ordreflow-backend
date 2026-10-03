namespace WebAPI.Contracts.Tasks;

public sealed record TaskResponse(
    Guid Id,
    Guid OrderId,
    string Title,
    string Description);
