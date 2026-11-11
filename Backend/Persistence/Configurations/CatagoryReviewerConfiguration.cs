using Domain.Aggregate;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public sealed class CatagoryReviewerConfiguration : IEntityTypeConfiguration<CatagoryReviewer>
{
    public void Configure(EntityTypeBuilder<CatagoryReviewer> entity)
    {
        entity.ToTable("catagory_reviewers");
        entity.HasKey(x => new { x.EmployeeId, x.CatagoryId });

        entity.Property(x => x.EmployeeId).HasColumnName("employee_id");
        entity.Property(x => x.CatagoryId).HasColumnName("catagory_id");

        entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Catagory>().WithMany().HasForeignKey(x => x.CatagoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
