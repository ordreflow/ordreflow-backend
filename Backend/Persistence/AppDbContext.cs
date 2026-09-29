namespace Persistence;

using Microsoft.EntityFrameworkCore;
using Domain.Aggregate;
using Domain.Entities;


public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
//TODO: This configuration is commented out because it need to be changed to fit the new structure

    /*
    public DbSet<User> Users => Set<User>();

    public DbSet<Case> Cases => Set<Case>();

    public DbSet<WorkCase> WorkItems => Set<WorkCase>();

    public DbSet<TimeSheet> TimeSheets => Set<TimeSheet>();

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration in Persistence/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
    */
}