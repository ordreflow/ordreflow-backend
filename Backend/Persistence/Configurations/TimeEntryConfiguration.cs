namespace Persistence.Configurations;

using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> entity)
    {
        entity.ToTable("time_entries");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        entity.Property(x => x.WorkId)
            .HasColumnName("work_item_id")
            .HasConversion(id => id.Value, value => WorkId.Create(value).Value)
            .IsRequired();

        entity.Property(x => x.TimeSheetId)
            .HasColumnName("time_sheet_id")
            .HasConversion(id => id!.Value, value => TimeSheetId.Create(value).Value);

        entity.Property(x => x.Date)
            .HasColumnName("date")
            .HasColumnType("date")
            .IsRequired();

        entity.Property(x => x.Hours)
            .HasColumnName("hours")
            .HasPrecision(5, 2)
            .IsRequired();

        entity.Property(x => x.StartTime)
            .HasColumnName("start_time")
            .HasColumnType("time");

        entity.Property(x => x.EndTime)
            .HasColumnName("end_time")
            .HasColumnType("time");

        entity.Property(x => x.Comment)
            .HasColumnName("comment")
            .HasMaxLength(1000);

        entity.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // A work item with registered time cannot be deleted, so history is kept.
        entity.HasOne<WorkCase>()
            .WithMany()
            .HasForeignKey(x => x.WorkId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
