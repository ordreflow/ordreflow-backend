namespace Persistence.Configurations;

using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TimeEntryReviewConfiguration : IEntityTypeConfiguration<TimeEntryReview>
{
    public void Configure(EntityTypeBuilder<TimeEntryReview> entity)
    {
        entity.ToTable("time_entry_reviews");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => TimeEntryReviewID.Create(value).Value)
            .ValueGeneratedNever();

        entity.Property(x => x.TimeEntryId)
            .HasColumnName("time_entry_id")
            .HasConversion(id => id.Value, value => TimeEntryId.Create(value).Value)
            .IsRequired();

        entity.Property(x => x.Decision)
            .HasColumnName("decision")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        entity.Property(x => x.Reason)
            .HasColumnName("reason");

        entity.Property(x => x.ReviewedAt)
            .HasColumnName("reviewed_at")
            .IsRequired();
    }
}
