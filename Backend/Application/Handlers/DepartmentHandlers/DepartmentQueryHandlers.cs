using Application.Dtos;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.Aggregate;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class GetDepartmentsHandler(
    IUserRepository userRepository,
    IDepartmentRepository departmentRepository) : IQueryHandler<
        GetDepartmentsQuery,
        Result<IReadOnlyList<DepartmentDto>>>
{
    public async Task<Result<IReadOnlyList<DepartmentDto>>> HandleAsync(GetDepartmentsQuery query)
    {
        var actorId = UserId.Create(query.ActorId);
        if (actorId.IsFailure)
            return Result<IReadOnlyList<DepartmentDto>>.Failure(actorId.Errors.ToArray());

        var actor = await userRepository.GetAsync(actorId.Value);
        if (actor is null || actor.Status != UserStatus.Active)
            return Result<IReadOnlyList<DepartmentDto>>.Failure(
                new Error("DepartmentAccessForbidden", "You are not authorized to view departments."));

        var isAdmin = actor.Role == UserRole.Admin;
        var departments = isAdmin
            ? await departmentRepository.GetAllAsync()
            : await departmentRepository.GetByMemberAsync(actor.UserId);

        var users = (await userRepository.GetAllAsync()).ToDictionary(user => user.UserId);

        var result = departments
            .OrderBy(department => department.Name.Value, StringComparer.CurrentCultureIgnoreCase)
            .Select(department =>
            {
                var managedByMe = DepartmentScope.Manages(actor, department);

                // Members are visible to admins and to the department's managers. Others only see themselves.
                var visibleMembers = isAdmin || managedByMe
                    ? department.Members
                    : department.Members.Where(member => member.UserId == actor.UserId);

                var members = visibleMembers
                    .Where(member => users.ContainsKey(member.UserId))
                    .Select(member => ToMemberDto(users[member.UserId], member.IsManager))
                    .OrderByDescending(member => member.IsManager)
                    .ThenBy(member => member.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToArray();

                return new DepartmentDto(
                    department.Id.Value,
                    department.Name.Value,
                    department.CreatedAt,
                    managedByMe,
                    members);
            })
            .ToArray();

        return Result<IReadOnlyList<DepartmentDto>>.Success(result);
    }

    private static DepartmentMemberDto ToMemberDto(User user, bool isManager) => new(
        user.UserId.Value,
        user.Name.Value,
        user.Email.Value,
        user.Role.ToString(),
        user.Status.ToString(),
        user.JobTitle?.Value,
        isManager);
}

public sealed class GetManagedEmployeesHandler(
    IUserRepository userRepository,
    IDepartmentRepository departmentRepository) : IQueryHandler<
        GetManagedEmployeesQuery,
        Result<IReadOnlyList<UserDto>>>
{
    public async Task<Result<IReadOnlyList<UserDto>>> HandleAsync(GetManagedEmployeesQuery query)
    {
        var actorId = UserId.Create(query.ActorId);
        if (actorId.IsFailure)
            return Result<IReadOnlyList<UserDto>>.Failure(actorId.Errors.ToArray());

        var actor = await userRepository.GetAsync(actorId.Value);
        if (actor is null)
            return Result<IReadOnlyList<UserDto>>.Failure(
                new Error("UserNotFound", "The current user could not be found."));

        if (!DepartmentScope.HasManagerAccess(actor))
            return Result<IReadOnlyList<UserDto>>.Failure(
                new Error("ManagedEmployeesForbidden", "Only active managers and admins have managed employees."));

        var managedDepartments = (await departmentRepository.GetByMemberAsync(actor.UserId))
            .Where(department => department.IsManagedBy(actor.UserId));

        var memberIds = managedDepartments
            .SelectMany(department => department.Members)
            .Select(member => member.UserId)
            .Where(memberId => memberId != actor.UserId)
            .ToHashSet();

        var employees = (await userRepository.GetAllAsync())
            .Where(user => memberIds.Contains(user.UserId) && user.Status == UserStatus.Active)
            .OrderBy(user => user.Name.Value, StringComparer.CurrentCultureIgnoreCase)
            .Select(user => new UserDto(
                user.UserId.Value,
                user.Name.Value,
                user.Email.Value,
                user.Role.ToString(),
                user.Status.ToString(),
                user.CreatedAt,
                user.ManagerId?.Value,
                user.JobTitle?.Value))
            .ToArray();

        return Result<IReadOnlyList<UserDto>>.Success(employees);
    }
}

/// <summary>
/// Department management only counts for an active user who still has manager or admin system access. A job title
/// never grants it (SEC-01), and a manager whose access is later removed loses the department scope immediately.
/// </summary>
public static class DepartmentScope
{
    public static bool HasManagerAccess(User user) =>
        user.Status == UserStatus.Active && user.Role is (UserRole.Manager or UserRole.Admin);

    public static bool Manages(User user, Department department) =>
        HasManagerAccess(user) && department.IsManagedBy(user.UserId);
}
