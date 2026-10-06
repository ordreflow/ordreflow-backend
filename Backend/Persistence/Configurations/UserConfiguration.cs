namespace Persistence.Configurations;

using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.ToTable("users");

        entity.HasKey(x => x.UserId);

        entity.Property(x => x.UserId)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => UserId.Create(value).Value)
            .ValueGeneratedNever();

        entity.Property(x => x.ManagerId)
            .HasColumnName("manager_id")
            .HasConversion(id => id!.Value, value => UserId.Create(value).Value);

        entity.Property(x => x.Name)
            .HasColumnName("name")
            .HasConversion(name => name.Value, value => PersonName.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(x => x.Email)
            .HasColumnName("email")
            .HasConversion(email => email.Value, value => EmailAddress.Create(value).Value)
            .HasMaxLength(320)
            .IsRequired();

        entity.HasIndex(x => x.Email)
            .IsUnique();

        entity.Property(x => x.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        entity.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // A user can have a manager, who is also a user.
        entity.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
