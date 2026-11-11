using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Aggregate;

public sealed class Department
{
    public DepartmentId Id { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public DepartmentStatus Status { get; private set; }

    public bool IsManagedBy(
        UserId actorId,
        UserRole actorRole,
        UserStatus actorStatus,
        DepartmentId? actorDepartmentId)
    {
        if (Status != DepartmentStatus.Active ||
            actorId is null ||
            actorStatus != UserStatus.Active)
            return false;

        if (actorRole == UserRole.Admin)
            return true;

        return actorRole == UserRole.Manager && actorDepartmentId == Id;
    }

    private Department()
    {
    }

    private Department(string name)
    {
        Id = DepartmentId.Create(Guid.NewGuid()).Value;
        Name = name;
        Status = DepartmentStatus.Active;
    }

    public static Result<Department> Create(string name)
    {
        var normalizedName = NormalizeName(name);
        return normalizedName.IsFailure
            ? Result<Department>.Failure(normalizedName.Errors.ToArray())
            : Result<Department>.Success(new Department(normalizedName.Value));
    }

    public Result Rename(string name)
    {
        if (Status != DepartmentStatus.Active)
            return Result.Failure(new Error("DepartmentInactive", "An inactive department cannot be renamed."));

        var normalizedName = NormalizeName(name);
        if (normalizedName.IsFailure)
            return Result.Failure(normalizedName.Errors.ToArray());

        Name = normalizedName.Value;
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (Status == DepartmentStatus.Inactive)
            return Result.Failure(new Error("DepartmentAlreadyInactive", "The department is already inactive."));

        Status = DepartmentStatus.Inactive;
        return Result.Success();
    }

    public Result Activate()
    {
        if (Status == DepartmentStatus.Active)
            return Result.Failure(new Error("DepartmentAlreadyActive", "The department is already active."));

        Status = DepartmentStatus.Active;
        return Result.Success();
    }

    private static Result<string> NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<string>.Failure(new Error("DepartmentNameRequired", "Department name is required."));

        name = name.Trim();
        return name.Length > 200
            ? Result<string>.Failure(new Error("DepartmentNameTooLong", "Department name cannot exceed 200 characters."))
            : Result<string>.Success(name);
    }
}
