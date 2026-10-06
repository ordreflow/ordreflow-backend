namespace Persistence.Configurations;

using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> entity)
    {
        entity.ToTable("orders");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => OrderId.Create(value).Value)
            .ValueGeneratedNever();

        entity.Property(x => x.ManagerId)
            .HasColumnName("manager_id")
            .HasConversion(id => id.Value, value => UserId.Create(value).Value)
            .IsRequired();

        entity.Property(x => x.Name)
            .HasColumnName("name")
            .HasConversion(name => name.Value, value => OrderName.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        entity.Property(x => x.ClosedAt)
            .HasColumnName("closed_at");

        // A manager who owns orders cannot be deleted.
        entity.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tasks live inside the order aggregate and are removed together with it.
        entity.HasMany(x => x.Tasks)
            .WithOne()
            .HasForeignKey(x => x.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        entity.Navigation(x => x.Tasks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
