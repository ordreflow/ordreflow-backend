namespace Persistence.Configurations;

using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CaseConfiguration : IEntityTypeConfiguration<Case>
{
    public void Configure(EntityTypeBuilder<Case> entity)
    {
        entity.ToTable("cases");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => CaseId.Create(value).Value)
            .ValueGeneratedNever();

        entity.Property(x => x.Name)
            .HasColumnName("name")
            .HasConversion(name => name.Value, value => CaseName.Create(value).Value)
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

        // Work items live inside the case aggregate and are removed together with it.
        entity.HasMany(x => x.WorkItems)
            .WithOne()
            .HasForeignKey(x => x.CaseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        entity.Navigation(x => x.WorkItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
