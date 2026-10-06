namespace Persistence.Configurations;

using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Task = Domain.Entities.Task;

public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> entity)
    {
        entity.ToTable("time_entries");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => TimeEntryId.Create(value).Value)
            .ValueGeneratedNever();

        entity.Property(x => x.EmployeeId)
            .HasColumnName("employee_id")
            .HasConversion(id => id.Value, value => UserId.Create(value).Value)
            .IsRequired();

        entity.Property(x => x.TaskId)
            .HasColumnName("task_id")
            .HasConversion(id => id.Value, value => TaskId.Create(value).Value)
            .IsRequired();

        entity.Property(x => x.Date)
            .HasColumnName("date")
            .HasColumnType("date")
            .IsRequired();

        entity.Property(x => x.Hours)
            .HasColumnName("hours")
            .HasPrecision(5, 2)
            .IsRequired();

        entity.Property(x => x.Comment)
            .HasColumnName("comment")
            .HasMaxLength(1000);

        entity.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // An employee with registered time cannot be deleted, so history is kept.
        entity.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // A task with registered time cannot be deleted, so history is kept.
        entity.HasOne<Task>()
            .WithMany()
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Restrict);

        // Reviews live inside the time entry aggregate and are removed together with it.
        entity.HasMany(x => x.Reviews)
            .WithOne()
            .HasForeignKey(x => x.TimeEntryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        entity.Navigation(x => x.Reviews)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
