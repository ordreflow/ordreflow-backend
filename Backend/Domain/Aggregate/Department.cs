using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Aggregate;

/// <summary>
/// A department that users belong to (FR-14). Users can belong to several departments, and a member can be a
/// manager of the department. Department management is a separate grant from the user's displayed job title
/// (FR-13): only users with manager or admin system access can manage a department, and only an active
/// administrator can change departments and their members.
/// </summary>
public sealed class Department
{
	private readonly List<DepartmentMember> _members = new();

	public DepartmentId Id { get; private set; } = null!;
	public DepartmentName Name { get; private set; } = null!;
	public DateTime CreatedAt { get; private set; }
	public IReadOnlyCollection<DepartmentMember> Members => _members.AsReadOnly();

	private Department()
	{
	}

	private Department(DepartmentName name)
	{
		Id = DepartmentId.Create(Guid.NewGuid()).Value;
		Name = name;
		CreatedAt = DateTime.UtcNow;
	}

	public static Result<Department> Create(UserRole actorRole, UserStatus actorStatus, DepartmentName name)
	{
		if (!IsActiveAdmin(actorRole, actorStatus))
			return Result<Department>.Failure(ManagementForbidden);

		if (name is null)
			return Result<Department>.Failure(new Error("DepartmentNameRequired", "Department name is required."));

		return Result<Department>.Success(new Department(name));
	}

	public Result Rename(UserRole actorRole, UserStatus actorStatus, DepartmentName name)
	{
		if (!IsActiveAdmin(actorRole, actorStatus))
			return Result.Failure(ManagementForbidden);

		if (name is null)
			return Result.Failure(new Error("DepartmentNameRequired", "Department name is required."));

		Name = name;
		return Result.Success();
	}

	/// <summary>Adds the user to the department, or changes whether an existing member manages it.</summary>
	public Result SetMember(
		UserRole actorRole,
		UserStatus actorStatus,
		UserId userId,
		UserRole userRole,
		UserStatus userStatus,
		bool isManager)
	{
		if (!IsActiveAdmin(actorRole, actorStatus))
			return Result.Failure(ManagementForbidden);

		if (userId is null)
			return Result.Failure(new Error("UserNotFound", "The user could not be found."));

		if (userStatus != UserStatus.Active)
			return Result.Failure(new Error("DepartmentMemberInactive", "Only active users can be added to a department."));

		if (isManager && userRole is not (UserRole.Manager or UserRole.Admin))
			return Result.Failure(new Error("DepartmentManagerAccessRequired",
				"Only users with manager or admin access can manage a department."));

		var member = _members.FirstOrDefault(existing => existing.UserId == userId);
		if (member is null)
			_members.Add(new DepartmentMember(userId, isManager));
		else
			member.SetManager(isManager);

		return Result.Success();
	}

	public Result RemoveMember(UserRole actorRole, UserStatus actorStatus, UserId userId)
	{
		if (!IsActiveAdmin(actorRole, actorStatus))
			return Result.Failure(ManagementForbidden);

		var member = _members.FirstOrDefault(existing => existing.UserId == userId);
		if (member is null)
			return Result.Failure(new Error("DepartmentMemberNotFound", "The user is not a member of the department."));

		_members.Remove(member);
		return Result.Success();
	}

	public bool HasMember(UserId userId) => _members.Any(member => member.UserId == userId);

	public bool IsManagedBy(UserId userId) => _members.Any(member => member.UserId == userId && member.IsManager);

	private static bool IsActiveAdmin(UserRole role, UserStatus status) =>
		role == UserRole.Admin && status == UserStatus.Active;

	private static readonly Error ManagementForbidden =
		new("DepartmentManagementForbidden", "Only an active admin can manage departments.");
}
