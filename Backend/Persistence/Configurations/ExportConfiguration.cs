using Domain.Aggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public sealed class ExportConfiguration : IEntityTypeConfiguration<Export>
{
    public void Configure(EntityTypeBuilder<Export> entity)
    {
        entity.ToTable("exports");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(x => x.ExportedBy).HasColumnName("exported_by").IsRequired();
        entity.Property(x => x.CreatedAt).HasColumnName("created_at")
            .HasColumnType("timestamp with time zone").IsRequired();
        entity.Property(x => x.DateFrom).HasColumnName("date_from").HasColumnType("date");
        entity.Property(x => x.DateTo).HasColumnName("date_to").HasColumnType("date");

        entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.ExportedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
