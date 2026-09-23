using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Aggregate;

public sealed class User
{
	public UserId UserId { get; private set; } = null!;
	public UserId? ManagerId { get; private set; }
	public PersonName Name { get; private set; } = null!;
	public EmailAddress Email { get; private set; } = null!;
	public UserRole Role { get; private set; }
	public UserStatus Status { get; private set; }
	public DateTime CreatedAt { get; private set; }

	private User()
	{
	}

	private User(PersonName name, EmailAddress email, UserRole role)
	{
		UserId = UserId.Create(Guid.NewGuid()).Value;
		Name = name;
		Email = email;
		Role = role;
		Status = UserStatus.Active;
		CreatedAt = DateTime.UtcNow;
	}

	public static Result<User> Create(PersonName name, EmailAddress email)
	{
		if (name is null)
			return Result<User>.Failure(new Error("NameRequired", "User name is required."));

		if (email is null)
			return Result<User>.Failure(new Error("EmailRequired", "User email is required."));

		return Result<User>.Success(new User(name, email, UserRole.Employee));
	}

	public static Result<User> Create(
		PersonName name,
		EmailAddress email,
		UserRole role)
	{
		if (name is null)
			return Result<User>.Failure(new Error("NameRequired", "User name is required."));

		if (email is null)
			return Result<User>.Failure(new Error("EmailRequired", "User email is required."));

		return Result<User>.Success(new User(name, email, role));
	}

	public Result<User> CreateUser(
		PersonName name,
		EmailAddress email,
		UserRole role)
	{
		if (!CanManageUsers)
			return Result<User>.Failure(new Error("UserCreationForbidden", "Only managers and admins can create users."));

		if (Role == UserRole.Manager && role != UserRole.Employee)
			return Result<User>.Failure(new Error("RoleCreationForbidden", "Managers can only create employees."));

		return Create(name, email, role);
	}

	public bool CanManageUsers => Status == UserStatus.Active &&
		(Role == UserRole.Manager || Role == UserRole.Admin);

	public bool CanManageCases => CanManageUsers;

	public Result ChangeRole(
		UserRole actorRole,
		UserStatus actorStatus,
		UserRole role)
	{
		if (actorRole != UserRole.Admin || actorStatus != UserStatus.Active)
			return Result.Failure(new Error("RoleChangeForbidden", "Only an active admin can change roles."));

		if (role is not UserRole.Employee and not UserRole.Manager and not UserRole.Admin)
			return Result.Failure(new Error("InvalidRole", "The selected user role is invalid."));

		Role = role;
		return Result.Success();
	}

	public Result AssignManager(
		UserId managerId,
		UserRole actorRole,
		UserStatus actorStatus)
	{
		if (actorRole != UserRole.Admin || actorStatus != UserStatus.Active)
			return Result.Failure(new Error("ManagerAssignmentForbidden", "Only an active admin can assign managers."));

		if (managerId is null || managerId == UserId)
			return Result.Failure(new Error("InvalidManager", "A user cannot be assigned to itself as manager."));

		ManagerId = managerId;
		return Result.Success();
	}

	public Result ChangeName(PersonName name)
	{
		if (name is null)
			return Result.Failure(new Error("NameRequired", "User name is required."));

		Name = name;
		return Result.Success();
	}

	public Result ChangeEmail(EmailAddress email)
	{
		if (email is null)
			return Result.Failure(new Error("EmailRequired", "User email is required."));

		Email = email;
		return Result.Success();
	}

	public Result Deactivate()
	{
		if (Status == UserStatus.Inactive)
			return Result.Failure(new Error("UserAlreadyInactive", "User is already inactive."));

		Status = UserStatus.Inactive;
		return Result.Success();
	}

	public Result Activate()
	{
		if (Status == UserStatus.Active)
			return Result.Failure(new Error("UserAlreadyActive", "User is already active."));

		Status = UserStatus.Active;
		return Result.Success();
	}
}