using Domain.Aggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public sealed class AccessPrivilegeConfiguration : IEntityTypeConfiguration<AccessPrivilege>
{
    public void Configure(EntityTypeBuilder<AccessPrivilege> entity)
    {
        entity.ToTable("access_privileges");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(x => x.IssuedBy).HasColumnName("issued_by").IsRequired();
        entity.Property(x => x.IssuedTo).HasColumnName("issued_to").IsRequired();
        entity.Property(x => x.IssuedAtUtc).HasColumnName("issued_at_utc")
            .HasColumnType("timestamp with time zone").IsRequired();
        entity.Property(x => x.EarliestWorkDate).HasColumnName("earliest_work_date")
            .HasColumnType("date");
        entity.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc")
            .HasColumnType("timestamp with time zone").IsRequired();

        entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.IssuedBy)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.IssuedTo)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
