using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Aggregate;

public sealed class User
{
    public UserId UserId { get; private set; } = null!;
    public PersonName Name { get; private set; } = null!;
    public EmailAddress Email { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    public DepartmentId? DepartmentId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User()
    {
    }

    private User(PersonName name, EmailAddress email, UserRole role, DepartmentId? departmentId)
    {
        UserId = UserId.Create(Guid.NewGuid()).Value;
        Name = name;
        Email = email;
        Role = role;
        Status = UserStatus.Active;
        DepartmentId = departmentId;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<User> Create(PersonName name, EmailAddress email) =>
        Result<User>.Failure(new Error(
            "DepartmentRequired",
            "Employees and managers must belong to a department."));

    public static Result<User> Create(
        PersonName name,
        EmailAddress email,
        UserRole role,
        DepartmentId? departmentId = null)
    {
        if (name is null)
            return Result<User>.Failure(new Error("NameRequired", "User name is required."));

        if (email is null)
            return Result<User>.Failure(new Error("EmailRequired", "User email is required."));

        if (!Enum.IsDefined(role))
            return Result<User>.Failure(new Error("InvalidRole", "The selected user role is invalid."));

        if ((role == UserRole.Admin && departmentId is not null) ||
            (role != UserRole.Admin && departmentId is null))
            return Result<User>.Failure(new Error(
                "DepartmentRoleMismatch",
                "Admins are global; employees and managers must belong to exactly one department."));

        return Result<User>.Success(new User(name, email, role, departmentId));
    }

    public Result<User> CreateUser(
        PersonName name,
        EmailAddress email,
        UserRole role,
        DepartmentId? departmentId)
    {
        if (Status != UserStatus.Active || Role != UserRole.Admin)
            return Result<User>.Failure(new Error(
                "UserCreationForbidden",
                "Only an active admin can create users."));

        return Create(name, email, role, departmentId);
    }

    public bool CanManageUsers => Status == UserStatus.Active && Role == UserRole.Admin;

    public bool CanReviewTimeEntries =>
        Status == UserStatus.Active && Role is UserRole.Manager or UserRole.Admin;

    public bool CanView(
        UserId actorId,
        UserRole actorRole,
        UserStatus actorStatus,
        DepartmentId? actorDepartmentId)
    {
        if (actorId is null || actorStatus != UserStatus.Active)
            return false;

        if (actorId == UserId || actorRole == UserRole.Admin)
            return true;

        return actorRole == UserRole.Manager &&
            actorDepartmentId is not null &&
            actorDepartmentId == DepartmentId;
    }

    public Result ChangeRole(
        UserId actorId,
        UserRole actorRole,
        UserStatus actorStatus,
        DepartmentId? actorDepartmentId,
        UserRole role,
        DepartmentId? departmentId)
    {
        if (actorId is null || actorRole != UserRole.Admin || actorStatus != UserStatus.Active)
            return Result.Failure(new Error(
                "RoleChangeForbidden",
                "Only an active admin can change roles."));

        if (!Enum.IsDefined(role))
            return Result.Failure(new Error("InvalidRole", "The selected user role is invalid."));

        if ((role == UserRole.Admin && departmentId is not null) ||
            (role != UserRole.Admin && departmentId is null))
            return Result.Failure(new Error(
                "DepartmentRoleMismatch",
                "Admins are global; employees and managers must belong to exactly one department."));

        Role = role;
        DepartmentId = departmentId;
        return Result.Success();
    }

    public Result ChangeName(
        UserId actorId,
        UserRole actorRole,
        UserStatus actorStatus,
        DepartmentId? actorDepartmentId,
        PersonName name)
    {
        if (!CanView(actorId, actorRole, actorStatus, actorDepartmentId))
            return Result.Failure(new Error(
                "ProfileEditForbidden",
                "You are not authorized to edit this user's profile."));

        if (name is null)
            return Result.Failure(new Error("NameRequired", "User name is required."));

        Name = name;
        return Result.Success();
    }

    public Result ChangeEmail(
        UserId actorId,
        UserRole actorRole,
        UserStatus actorStatus,
        DepartmentId? actorDepartmentId,
        EmailAddress email)
    {
        if (!CanView(actorId, actorRole, actorStatus, actorDepartmentId))
            return Result.Failure(new Error(
                "ProfileEditForbidden",
                "You are not authorized to edit this user's profile."));

        if (email is null)
            return Result.Failure(new Error("EmailRequired", "User email is required."));

        Email = email;
        return Result.Success();
    }

    public Result Deactivate(
        UserId actorId,
        UserRole actorRole,
        UserStatus actorStatus,
        DepartmentId? actorDepartmentId)
    {
        if (actorId is null || actorRole != UserRole.Admin || actorStatus != UserStatus.Active)
            return Result.Failure(new Error(
                "DeactivationForbidden",
                "Only an active admin can deactivate users."));

        if (Status == UserStatus.Inactive)
            return Result.Failure(new Error("UserAlreadyInactive", "User is already inactive."));

        Status = UserStatus.Inactive;
        return Result.Success();
    }

    public Result Activate(
        UserId actorId,
        UserRole actorRole,
        UserStatus actorStatus,
        DepartmentId? actorDepartmentId)
    {
        if (actorId is null || actorRole != UserRole.Admin || actorStatus != UserStatus.Active)
            return Result.Failure(new Error(
                "ActivationForbidden",
                "Only an active admin can activate users."));

        if (Status == UserStatus.Active)
            return Result.Failure(new Error("UserAlreadyActive", "User is already active."));

        Status = UserStatus.Active;
        return Result.Success();
    }
}
