using Domain.ValueObjects;

namespace Domain.Entities;

// A user's membership of a department. Managers of a department plan work for its members (FR-14).
public sealed class DepartmentMember
{
	public UserId UserId { get; private set; } = null!;
	public bool IsManager { get; private set; }

	private DepartmentMember()
	{
	}

	internal DepartmentMember(UserId userId, bool isManager)
	{
		UserId = userId;
		IsManager = isManager;
	}

	internal void SetManager(bool isManager) => IsManager = isManager;
}
