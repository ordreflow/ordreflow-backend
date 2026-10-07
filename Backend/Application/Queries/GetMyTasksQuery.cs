using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Queries;

public sealed record GetMyTasksQuery(UserId EmployeeId);
