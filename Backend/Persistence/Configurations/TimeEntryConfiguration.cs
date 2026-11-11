namespace Persistence.Configurations;

using Domain.Aggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> entity)
    {
        entity.ToTable("time_entries");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        entity.Property(x => x.EmployeeId)
            .HasColumnName("employee_id")
            .IsRequired();

        entity.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(x => x.Audits)
            .WithOne()
            .HasForeignKey(x => x.TimeEntryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        entity.Navigation(x => x.Audits)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
