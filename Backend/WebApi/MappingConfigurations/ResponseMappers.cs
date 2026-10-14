using Application.Dtos;
using ObjectMapper;
using WebAPI.Contracts.Orders;
using WebAPI.Contracts.Tasks;
using WebAPI.Contracts.TimeEntries;
using WebAPI.Contracts.Users;

namespace WebAPI.MappingConfigurations;

public sealed class OrderToResponseMapper
    : IMappingConfig<OrderDto, OrderResponse>
{
    public OrderResponse Map(OrderDto input) => new(
        input.Id,
        input.Name,
        input.Status,
        input.CreatedAt,
        input.ClosedAt);
}

public sealed class TaskToResponseMapper
    : IMappingConfig<TaskDto, TaskResponse>
{
    public TaskResponse Map(TaskDto input) => new(
        input.Id,
        input.OrderId,
        input.Title,
        input.Description);
}

public sealed class TimeEntryToResponseMapper
    : IMappingConfig<TimeEntryDto, TimeEntryResponse>
{
    public TimeEntryResponse Map(TimeEntryDto input) => new(
        input.Id,
        input.TaskId,
        input.Date,
        input.Hours,
        input.Comment);
}

public sealed class UserToResponseMapper
    : IMappingConfig<UserDto, UserResponse>
{
    public UserResponse Map(UserDto input) => new(
        input.Id,
        input.Name,
        input.Email,
        input.Role,
        input.Status,
        input.CreatedAt);
}
