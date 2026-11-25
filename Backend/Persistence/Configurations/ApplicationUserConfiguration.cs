namespace Persistence.Configurations;

using Domain.Aggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Persistence.Identity;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> entity)
    {
        entity.Property(x => x.EmployeeId)
            .IsRequired();

        // Each employee has at most one Identity login account.
        entity.HasOne<Employee>()
            .WithOne()
            .HasForeignKey<ApplicationUser>(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
