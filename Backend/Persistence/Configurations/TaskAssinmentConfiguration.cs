using Domain.Aggregate;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Task = Domain.Entities.Task;

namespace Persistence.Configurations;

public sealed class TaskAssinmentConfiguration : IEntityTypeConfiguration<TaskAssinment>
{
    public void Configure(EntityTypeBuilder<TaskAssinment> entity)
    {
        entity.ToTable("task_assinments");
        entity.HasKey(x => new { x.TaskId, x.AssignedTo });

        entity.Property(x => x.TaskId).HasColumnName("task_id");
        entity.Property(x => x.AssignedTo).HasColumnName("assigned_to");
        entity.Property(x => x.ExpectedDuration).HasColumnName("expected_duration")
            .HasColumnType("double precision");
        entity.Property(x => x.ExpectedDeadline).HasColumnName("expected_deadline")
            .HasColumnType("timestamp with time zone");

        entity.HasOne<Task>().WithMany().HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.AssignedTo)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
