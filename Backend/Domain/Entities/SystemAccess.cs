using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class SystemAccess
{
    public SystemAccessId Id { get; private set; } = null!;
    public UserId UserId { get; private set; } = null!;
    public SystemAccessKind Kind { get; private set; }
    public bool Active { get; private set; }

    private SystemAccess()
    {
    }

    private SystemAccess(UserId userId, SystemAccessKind kind)
    {
        Id = SystemAccessId.Create(Guid.NewGuid()).Value;
        UserId = userId;
        Kind = kind;
        Active = true;
    }

    public static Result<SystemAccess> Create(UserId userId, SystemAccessKind kind)
    {
        if (userId is null)
            return Result<SystemAccess>.Failure(new Error("UserRequired", "A user is required."));

        if (!Enum.IsDefined(kind))
            return Result<SystemAccess>.Failure(new Error("InvalidSystemAccessKind", "The system access kind is invalid."));

        return Result<SystemAccess>.Success(new SystemAccess(userId, kind));
    }

    public Result ChangeKind(UserRole actorRole, SystemAccessKind kind)
    {
        if (actorRole != UserRole.Admin)
            return Result.Failure(new Error("SystemAccessChangeForbidden", "Only an admin can change system access."));

        if (!Active)
            return Result.Failure(new Error("SystemAccessInactive", "Inactive system access cannot be changed."));

        if (!Enum.IsDefined(kind))
            return Result.Failure(new Error("InvalidSystemAccessKind", "The system access kind is invalid."));

        Kind = kind;
        return Result.Success();
    }

    public Result Revoke(UserRole actorRole)
    {
        if (actorRole != UserRole.Admin)
            return Result.Failure(new Error("SystemAccessChangeForbidden", "Only an admin can revoke system access."));

        if (!Active)
            return Result.Failure(new Error("SystemAccessAlreadyRevoked", "System access is already revoked."));

        Active = false;
        return Result.Success();
    }
}
