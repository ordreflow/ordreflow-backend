namespace WebAPI.Contracts.Tasks;

public sealed record CreateTaskRequest(
    Guid OrderId,
    string Title,
    string Description);
