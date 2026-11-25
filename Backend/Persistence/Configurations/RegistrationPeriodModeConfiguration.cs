using Domain.Aggregate;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public sealed class RegistrationPeriodModeConfiguration : IEntityTypeConfiguration<RegistrationPeriodMode>
{
    public void Configure(EntityTypeBuilder<RegistrationPeriodMode> entity)
    {
        entity.ToTable("registration_period_modes", table =>
            table.HasCheckConstraint("CK_registration_period_modes_day_in_month",
                "day_in_month IS NULL OR day_in_month BETWEEN 1 AND 28"));
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        entity.Property(x => x.AdminId).HasColumnName("admin_id").IsRequired();
        entity.Property(x => x.Interval).HasColumnName("interval")
            .HasConversion(value => RegistrationIntervalStorage.ToDatabase(value),
                value => RegistrationIntervalStorage.FromDatabase(value))
            .IsRequired();
        entity.Property(x => x.DayInMonth).HasColumnName("day_in_month");
        entity.Property(x => x.CreatedDate).HasColumnName("created_date")
            .HasColumnType("timestamp with time zone").IsRequired();

        entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.AdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal static class RegistrationIntervalStorage
{
    public static string ToDatabase(RegistrationInterval value) => value switch
    {
        RegistrationInterval.Daily => "Daily",
        RegistrationInterval.Weekly => "Weekly",
        RegistrationInterval.Monthy => "Monthy",
        RegistrationInterval.DayInMonth => "Day-In-Month",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown registration interval")
    };

    public static RegistrationInterval FromDatabase(string value) => value switch
    {
        "Daily" => RegistrationInterval.Daily,
        "Weekly" => RegistrationInterval.Weekly,
        "Monthy" => RegistrationInterval.Monthy,
        "Day-In-Month" => RegistrationInterval.DayInMonth,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown registration interval")
    };
}
