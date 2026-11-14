using Domain.Aggregate;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Task = Domain.Entities.Task;

namespace Persistence.Configurations;

public sealed class TimeEntryAuditConfiguration : IEntityTypeConfiguration<TimeEntryAudit>
{
    public void Configure(EntityTypeBuilder<TimeEntryAudit> entity)
    {
        entity.ToTable("time_entry_audits");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(x => x.TimeEntryId).HasColumnName("time_entry_id").IsRequired();
        entity.Property(x => x.ActorId).HasColumnName("actor_id").IsRequired();
        entity.Property(x => x.ClosedBy).HasColumnName("closed_by");
        entity.Property(x => x.TaskId).HasColumnName("task_id").IsRequired();
        entity.Property(x => x.WorkDate).HasColumnName("work_date").HasColumnType("date");
        entity.Property(x => x.Duration).HasColumnName("duration").HasColumnType("double precision");
        entity.Property(x => x.Description).HasColumnName("description").IsRequired();
        entity.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired();
        entity.Property(x => x.AuditTime).HasColumnName("audit_time")
            .HasColumnType("timestamp with time zone").IsRequired();

        entity.HasIndex(x => new { x.TimeEntryId, x.AuditTime }).IsUnique();
        entity.HasIndex(x => new { x.TaskId, x.TimeEntryId });

        entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.ClosedBy)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Task>().WithMany().HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        // The TimeEntry/Audits relationship is configured on TimeEntry.
    }
}
