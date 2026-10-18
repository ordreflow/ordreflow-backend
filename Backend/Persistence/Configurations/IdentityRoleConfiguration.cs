namespace Persistence.Configurations;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Identity;

public class IdentityRoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> entity)
    {
        // Fixed ids and stamps keep the seeded roles stable between migrations.
        entity.HasData(
            CreateRole("0f3c2a6e-6d1b-4c8e-9a51-1b7f2e4d9c01", IdentityRoles.Employee),
            CreateRole("5a8e4b2d-3f7c-4e19-8b6a-2c9d1e7f4a02", IdentityRoles.Manager),
            CreateRole("9b1d7e3a-2c4f-4a8b-9e6d-3f5a2b8c1d03", IdentityRoles.Admin));
    }

    private static IdentityRole<Guid> CreateRole(string id, string name) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        ConcurrencyStamp = id
    };
}
