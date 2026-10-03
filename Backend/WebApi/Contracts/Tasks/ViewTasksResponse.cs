namespace WebAPI.Contracts.Tasks;

public sealed record ViewTasksResponse(
    IReadOnlyCollection<TaskResponse> Items);
