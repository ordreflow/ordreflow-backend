namespace WebAPI.Contracts.Tasks;

public sealed record UpdateTaskRequest(
    string Title,
    string Description);
