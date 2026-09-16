namespace Persistence;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;


public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TimeEntry>(entity =>
        {
            entity.ToTable("time_entries");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(x => x.Date)
                .HasColumnName("date")
                .IsRequired();

            entity.Property(x => x.Hours)
                .HasColumnName("hours")
                .HasPrecision(5, 2)
                .IsRequired();

            entity.Property(x => x.StartTime)
                .HasColumnName("start_time");

            entity.Property(x => x.EndTime)
                .HasColumnName("end_time");

            entity.Property(x => x.Comment)
                .HasColumnName("comment")
                .HasMaxLength(1000);
        });
    }
}