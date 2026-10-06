namespace Persistence;

using Microsoft.EntityFrameworkCore;
using Domain.Aggregate;
using Domain.Entities;
using Task = Domain.Entities.Task;


public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Task> Tasks => Set<Task>();

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    public DbSet<TimeEntryReview> TimeEntryReviews => Set<TimeEntryReview>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration in Persistence/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
