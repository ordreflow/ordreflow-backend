using Domain.Aggregate;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public sealed class DepartmentManagerConfiguration : IEntityTypeConfiguration<DepartmentManager>
{
    public void Configure(EntityTypeBuilder<DepartmentManager> entity)
    {
        entity.ToTable("department_managers");
        entity.HasKey(x => new { x.DepartmentId, x.ManagerId });

        entity.Property(x => x.DepartmentId).HasColumnName("department_id");
        entity.Property(x => x.ManagerId).HasColumnName("manager_id");

        entity.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
