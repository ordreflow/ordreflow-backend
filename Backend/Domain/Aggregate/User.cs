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

	public bool CanReviewTimeEntries => Status == UserStatus.Active &&
		(Role == UserRole.Manager || Role == UserRole.Admin);

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
		UserRole managerRole,
		UserStatus managerStatus,
		UserRole actorRole,
		UserStatus actorStatus)
	{
		if (actorRole != UserRole.Admin || actorStatus != UserStatus.Active)
			return Result.Failure(new Error("ManagerAssignmentForbidden", "Only an active admin can assign managers."));

		if (managerId is null || managerId == UserId)
			return Result.Failure(new Error("InvalidManager", "A user cannot be assigned to itself as manager."));

		if (managerStatus != UserStatus.Active ||
			managerRole is not (UserRole.Manager or UserRole.Admin))
			return Result.Failure(new Error("InvalidManager", "The selected manager is not active or authorized."));

		ManagerId = managerId;
		return Result.Success();
	}

	public bool CanView(
		UserId actorId,
		UserRole actorRole,
		UserStatus actorStatus)
	{
		if (actorStatus != UserStatus.Active)
			return false;

		if (actorId == UserId)
			return true;

		if (actorRole == UserRole.Admin)
			return true;

		return actorRole == UserRole.Manager && ManagerId == actorId;
	}

	public Result ChangeName(
		UserId actorId,
		UserRole actorRole,
		UserStatus actorStatus,
		PersonName name)
	{
		if (!CanView(actorId, actorRole, actorStatus))
			return Result.Failure(new Error("ProfileEditForbidden", "You are not authorized to edit this user's profile."));

		if (name is null)
			return Result.Failure(new Error("NameRequired", "User name is required."));

		Name = name;
		return Result.Success();
	}

	public Result ChangeEmail(
		UserId actorId,
		UserRole actorRole,
		UserStatus actorStatus,
		EmailAddress email)
	{
		if (!CanView(actorId, actorRole, actorStatus))
			return Result.Failure(new Error("ProfileEditForbidden", "You are not authorized to edit this user's profile."));

		if (email is null)
			return Result.Failure(new Error("EmailRequired", "User email is required."));

		Email = email;
		return Result.Success();
	}

	public Result Deactivate(UserRole actorRole, UserStatus actorStatus)
	{
		if (actorRole != UserRole.Admin || actorStatus != UserStatus.Active)
			return Result.Failure(new Error("DeactivationForbidden", "Only an active admin can deactivate users."));

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