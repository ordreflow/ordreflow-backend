using Domain.Aggregate;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public sealed class DepartmentEmployeeConfiguration : IEntityTypeConfiguration<DepartmentEmployee>
{
    public void Configure(EntityTypeBuilder<DepartmentEmployee> entity)
    {
        entity.ToTable("department_employees");
        entity.HasKey(x => new { x.DepartmentId, x.EmployeeId });

        entity.Property(x => x.DepartmentId).HasColumnName("department_id");
        entity.Property(x => x.EmployeeId).HasColumnName("employee_id");

        entity.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
