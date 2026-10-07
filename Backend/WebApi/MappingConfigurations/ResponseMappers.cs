using Domain.Aggregate;
using Domain.Entities;
using ObjectMapper;
using WebAPI.Contracts.Orders;
using WebAPI.Contracts.Tasks;
using WebAPI.Contracts.TimeEntries;
using WebAPI.Contracts.Users;
using TaskEntity = Domain.Entities.Task;

namespace WebAPI.MappingConfigurations;

public sealed class OrderToResponseMapper
    : IMappingConfig<Order, OrderResponse>
{
    public OrderResponse Map(Order input) => new(
        input.Id.Value,
        input.Name.Value,
        input.Status.ToString(),
        input.CreatedAt,
        input.ClosedAt);
}

public sealed class TaskToResponseMapper
    : IMappingConfig<TaskEntity, TaskResponse>
{
    public TaskResponse Map(TaskEntity input) => new(
        input.TaskId.Value,
        input.OrderId?.Value ?? Guid.Empty,
        input.Title,
        input.Description);
}

public sealed class TimeEntryToResponseMapper
    : IMappingConfig<TimeEntry, TimeEntryResponse>
{
    public TimeEntryResponse Map(TimeEntry input) => new(
        input.Id.Value,
        input.TaskId.Value,
        input.Date,
        input.Hours,
        input.Comment);
}

public sealed class UserToResponseMapper
    : IMappingConfig<User, UserResponse>
{
    public UserResponse Map(User input) => new(
        input.UserId.Value,
        input.Name.Value,
        input.Email.Value,
        input.Role.ToString(),
        input.Status.ToString(),
        input.CreatedAt);
}
