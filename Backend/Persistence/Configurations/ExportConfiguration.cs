using Domain.Aggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public sealed class ExportConfiguration : IEntityTypeConfiguration<Export>
{
    public void Configure(EntityTypeBuilder<Export> entity)
    {
        entity.ToTable("exports", table =>
        {
            table.HasCheckConstraint("CK_exports_dates", "date_from <= date_to");
            table.HasCheckConstraint("CK_exports_closure", "(time_entries_closed_by IS NULL) = (time_entries_closed_at_utc IS NULL)");
        });
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(x => x.ExportedBy).HasColumnName("exported_by").IsRequired();
        entity.Property(x => x.CreatedAt).HasColumnName("created_at")
            .HasColumnType("timestamp with time zone").IsRequired();
        entity.Property(x => x.DateFrom).HasColumnName("date_from").HasColumnType("date");
        entity.Property(x => x.DateTo).HasColumnName("date_to").HasColumnType("date");
        entity.Property(x => x.SelectedEmployeeId).HasColumnName("selected_employee_id");
        entity.Property(x => x.TotalHours).HasColumnName("total_hours").HasPrecision(18, 2);
        entity.Property(x => x.Revision).HasColumnName("revision").IsConcurrencyToken();
        entity.Property(x => x.TimeEntriesClosedBy).HasColumnName("time_entries_closed_by");
        entity.Property(x => x.TimeEntriesClosedAtUtc).HasColumnName("time_entries_closed_at_utc").HasColumnType("timestamp with time zone");
        entity.Property(x => x.CsvContent).HasField("_csvContent").UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("csv_content").IsRequired();
        entity.HasIndex(x => new { x.CreatedAt, x.Id });

        entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.ExportedBy)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.SelectedEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.TimeEntriesClosedBy)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasMany(x => x.TimeEntries)
            .WithOne()
            .HasForeignKey(x => x.ExportId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.Navigation(x => x.TimeEntries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
