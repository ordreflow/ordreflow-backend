using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Task = Domain.Entities.Task;

namespace Persistence.Configurations;

public sealed class AccessTaskConfiguration : IEntityTypeConfiguration<AccessTask>
{
    public void Configure(EntityTypeBuilder<AccessTask> entity)
    {
        entity.ToTable("access_tasks");
        entity.HasKey(x => new { x.AccessId, x.TaskId });

        entity.Property(x => x.AccessId).HasColumnName("access_id");
        entity.Property(x => x.TaskId).HasColumnName("task_id");

        entity.HasOne<Task>().WithMany().HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
