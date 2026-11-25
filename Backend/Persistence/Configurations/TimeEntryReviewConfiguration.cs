namespace Persistence.Configurations;

using Domain.Aggregate;
using Domain.Entities;
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
            .ValueGeneratedNever();

        entity.Property(x => x.TimeEntryId)
            .HasColumnName("time_entry_id")
            .IsRequired();

        entity.Property(x => x.ReviewerId)
            .HasColumnName("reviewer_id")
            .IsRequired();

        entity.Property(x => x.Description)
            .HasColumnName("description");

        entity.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        entity.Property(x => x.ReviewedAtUtc)
            .HasColumnName("reviewed_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        entity.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(x => x.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
