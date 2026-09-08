using Domain.ValueObjects;
using Core.Tools.OperationResult;

namespace Domain.Aggregate;

public class Tenant
{
    public TenantId Id { get; private set; } = null!;

    public CompanyName Name { get; private set; } = null!;

    public Subdomain Subdomain { get; private set; } = null!;

    public TenantStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private Tenant()
    {
        // Required by EF Core
    }

    private Tenant(
        TenantId id,
        CompanyName name,
        Subdomain subdomain)
    {
        Id = TenantId.Create(id.Value);
        Name = name;
        Subdomain = subdomain;
        Status = TenantStatus.Active;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Tenant> Create(
        CompanyName name,
        Subdomain subdomain)
    {
        if (name is null)
            return Result<Tenant>.Failure(new Error("CompanyNameRequired", "Company name is required."));

        if (subdomain is null)
            return Result<Tenant>.Failure(new Error("SubdomainRequired", "Subdomain is required."));

        return Result<Tenant>.Success(new Tenant(
            TenantId.Create(Guid.NewGuid()),
            name,
            subdomain));
    }

    public Result ChangeName(CompanyName newName)
    {
        if (newName == null)
        {
            return Result.Failure(new Error("InvalidCompanyName", "Company name cannot be null."));
        }

        Name = newName;
        return Result.Success();
    }

    public  Result ChangeSubdomain(Subdomain newSubdomain)
    {
        if (newSubdomain == null)
        {
            return Result.Failure(new Error("InvalidSubdomain", "Subdomain cannot be null."));
        }

        Subdomain = newSubdomain;
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (Status == TenantStatus.Inactive)
        {
            return Result.Failure(new Error("TenantAlreadyInactive", "Tenant is already inactive."));
        }

        Status = TenantStatus.Inactive;
        return Result.Success();
    }

    public Result Activate()
    {
        if (Status == TenantStatus.Active)
        {
            return Result.Failure(new Error("TenantAlreadyActive", "Tenant is already active."));
        }

        Status = TenantStatus.Active;
        return Result.Success();
    }

//suspend
    public Result Suspend()
    {
        if (Status == TenantStatus.Suspended)
        {
            return Result.Failure(new Error("TenantAlreadySuspended", "Tenant is already suspended."));
        }

        Status = TenantStatus.Suspended;
        return Result.Success();
    }
}