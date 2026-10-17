namespace Persistence.Configurations;

using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Identity;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> entity)
    {
        entity.Property(x => x.UserId)
            .HasConversion(id => id.Value, value => UserId.Create(value).Value)
            .IsRequired();

        // Each domain user has at most one login account.
        entity.HasOne<User>()
            .WithOne()
            .HasForeignKey<ApplicationUser>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
