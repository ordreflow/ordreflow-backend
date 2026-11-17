namespace Persistence.Configurations;

using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> entity)
    {
        entity.ToTable("departments");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => DepartmentId.Create(value).Value)
            .ValueGeneratedNever();

        entity.Property(x => x.Name)
            .HasColumnName("name")
            .HasConversion(name => name.Value, value => DepartmentName.Create(value).Value)
            .HasMaxLength(DepartmentName.MaxLength)
            .IsRequired();

        entity.HasIndex(x => x.Name)
            .IsUnique();

        entity.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Members live inside the department aggregate. A user can be a member of several departments.
        entity.OwnsMany(x => x.Members, member =>
        {
            member.ToTable("department_members");

            member.Property<DepartmentId>("DepartmentId")
                .HasColumnName("department_id")
                .HasConversion(id => id.Value, value => DepartmentId.Create(value).Value);

            member.WithOwner()
                .HasForeignKey("DepartmentId");

            member.Property(x => x.UserId)
                .HasColumnName("user_id")
                .HasConversion(id => id.Value, value => UserId.Create(value).Value)
                .IsRequired();

            member.Property(x => x.IsManager)
                .HasColumnName("is_manager")
                .IsRequired();

            member.HasKey("DepartmentId", nameof(Domain.Entities.DepartmentMember.UserId));

            member.HasIndex(x => x.UserId);

            // Users are deactivated rather than deleted, so a member user cannot be removed.
            member.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        entity.Navigation(x => x.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
