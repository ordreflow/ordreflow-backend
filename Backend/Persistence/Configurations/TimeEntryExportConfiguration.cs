using Domain.Aggregate;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public sealed class TimeEntryExportConfiguration : IEntityTypeConfiguration<TimeEntryExport>
{
    public void Configure(EntityTypeBuilder<TimeEntryExport> entity)
    {
        entity.ToTable("time_entry_exports");
        entity.HasKey(x => new { x.ExportId, x.TimeEntryId });

        entity.Property(x => x.ExportId).HasColumnName("export_id");
        entity.Property(x => x.TimeEntryId).HasColumnName("time_entry_id");
        entity.Property(x => x.TimeEntryAuditId).HasColumnName("time_entry_audit_id").IsRequired();

        entity.HasOne<TimeEntryAudit>().WithMany().HasForeignKey(x => x.TimeEntryAuditId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<TimeEntry>().WithMany().HasForeignKey(x => x.TimeEntryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
