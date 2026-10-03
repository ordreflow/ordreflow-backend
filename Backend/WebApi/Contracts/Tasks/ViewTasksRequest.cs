namespace WebAPI.Contracts.Tasks;

public sealed record ViewTasksRequest(
    Guid? OrderId);
