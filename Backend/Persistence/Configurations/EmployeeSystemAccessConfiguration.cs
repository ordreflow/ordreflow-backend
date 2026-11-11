using Domain.Aggregate;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public sealed class EmployeeSystemAccessConfiguration : IEntityTypeConfiguration<EmployeeSystemAccess>
{
    public void Configure(EntityTypeBuilder<EmployeeSystemAccess> entity)
    {
        entity.ToTable("employee_system_accesses");
        entity.HasKey(x => new { x.EmployeeId, x.Permission });

        entity.Property(x => x.EmployeeId).HasColumnName("employee_id");
        entity.Property(x => x.Permission).HasColumnName("permission")
            .HasConversion<string>().IsRequired();

        entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
