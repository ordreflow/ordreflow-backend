namespace Persistence.Configurations;

using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
//TODO: This configuration is commented out because it need to be changed to fit the new structure

/*
public class WorkCaseConfiguration : IEntityTypeConfiguration<WorkCase>
{
    public void Configure(EntityTypeBuilder<WorkCase> entity)
    {
        entity.ToTable("work_items");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => TaskId.Create(value).Value)
            .ValueGeneratedNever();

        entity.Property(x => x.CaseId)
            .HasColumnName("case_id")
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
*/