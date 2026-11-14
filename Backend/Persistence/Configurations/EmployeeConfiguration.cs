using Domain.Aggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> entity)
    {
        entity.ToTable("employees");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired();
        entity.Property(x => x.Title).HasColumnName("title");
        entity.Property(x => x.CreatedAt).HasColumnName("created_at")
            .HasColumnType("timestamp with time zone").IsRequired();

        entity.HasMany(x => x.Accesses)
            .WithOne()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.Navigation(x => x.Accesses)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
