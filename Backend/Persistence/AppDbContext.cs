namespace Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Domain.Aggregate;
using Domain.Entities;
using Persistence.Identity;
using Task = Domain.Entities.Task;


public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Domain users. Hides IdentityDbContext.Users; login accounts are reached through UserManager.
    public new DbSet<User> Users => Set<User>();

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
