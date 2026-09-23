namespace Persistence.Configurations;

using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
//TODO: This configuration is commented out because it need to be changed to fit the new structure

/*
public class TimeSheetConfiguration : IEntityTypeConfiguration<TimeSheet>
{
    public void Configure(EntityTypeBuilder<TimeSheet> entity)
    {
        entity.ToTable("time_sheets");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => TimeSheetId.Create(value).Value)
            .ValueGeneratedNever();

        entity.Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasConversion(id => id.Value, value => UserId.Create(value).Value)
            .IsRequired();

        entity.Property(x => x.Year)
            .HasColumnName("year")
            .IsRequired();

        entity.Property(x => x.Month)
            .HasColumnName("month")
            .IsRequired();

        entity.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // One timesheet per user per month.
        entity.HasIndex(x => new { x.UserId, x.Year, x.Month })
            .IsUnique();

        // A user with timesheets cannot be deleted, so registered time is kept as history.
        entity.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Entries live inside the timesheet aggregate and are removed together with it.
        entity.HasMany(x => x.Entries)
            .WithOne()
            .HasForeignKey(x => x.TimeSheetId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        entity.Navigation(x => x.Entries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
*/
