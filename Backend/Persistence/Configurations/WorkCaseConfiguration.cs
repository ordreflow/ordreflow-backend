namespace Persistence.Configurations;

using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class WorkCaseConfiguration : IEntityTypeConfiguration<WorkCase>
{
    public void Configure(EntityTypeBuilder<WorkCase> entity)
    {
        entity.ToTable("work_items");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => WorkId.Create(value).Value)
            .ValueGeneratedNever();

        entity.Property(x => x.CaseId)
            .HasColumnName("case_id")
            .HasConversion(id => id!.Value, value => CaseId.Create(value).Value);

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
