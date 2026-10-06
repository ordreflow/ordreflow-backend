namespace Persistence.Configurations;

using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Task = Domain.Entities.Task;

public class TaskConfiguration : IEntityTypeConfiguration<Task>
{
    public void Configure(EntityTypeBuilder<Task> entity)
    {
        entity.ToTable("tasks");

        entity.HasKey(x => x.TaskId);

        entity.Property(x => x.TaskId)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => TaskId.Create(value).Value)
            .ValueGeneratedNever();

        entity.Property(x => x.OrderId)
            .HasColumnName("order_id")
            .HasConversion(id => id!.Value, value => OrderId.Create(value).Value);

        entity.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(2000)
            .IsRequired();
    }
}
