using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Entities;

public sealed class LocalAccount
{
    public UserId UserId { get; private set; } = null!;
    public string ProviderSubject { get; private set; } = null!;
    public DateTime LinkedAt { get; private set; }
    public bool Active { get; private set; }

    private LocalAccount()
    {
    }

    private LocalAccount(UserId userId, string providerSubject)
    {
        UserId = userId;
        ProviderSubject = providerSubject;
        LinkedAt = DateTime.UtcNow;
        Active = true;
    }

    public static Result<LocalAccount> Create(UserId userId, string providerSubject)
    {
        if (userId is null)
            return Result<LocalAccount>.Failure(new Error("UserRequired", "A user is required."));

        if (string.IsNullOrWhiteSpace(providerSubject))
            return Result<LocalAccount>.Failure(new Error("ProviderSubjectRequired", "An account provider subject is required."));

        providerSubject = providerSubject.Trim();
        if (providerSubject.Length > 300)
            return Result<LocalAccount>.Failure(new Error("ProviderSubjectTooLong", "Provider subject cannot exceed 300 characters."));

        return Result<LocalAccount>.Success(new LocalAccount(userId, providerSubject));
    }

    public Result Deactivate()
    {
        if (!Active)
            return Result.Failure(new Error("LocalAccountAlreadyInactive", "The local account is already inactive."));

        Active = false;
        return Result.Success();
    }
}
