namespace Persistence;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Domain.Aggregate;
using Domain.Entities;
using Domain.ValueObjects;
using Persistence.Identity;
using Task = Domain.Entities.Task;


public class AppDbContext : IdentityUserContext<ApplicationUser, Guid>
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeSystemAccess> EmployeeSystemAccesses => Set<EmployeeSystemAccess>();
    public DbSet<Task> Tasks => Set<Task>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<TimeEntryReview> TimeEntryReviews => Set<TimeEntryReview>();
    public DbSet<TimeEntryAudit> TimeEntryAudits => Set<TimeEntryAudit>();
    public DbSet<Export> Exports => Set<Export>();
    public DbSet<TimeEntryExport> TimeEntryExports => Set<TimeEntryExport>();
    public DbSet<RegistrationPeriodMode> RegistrationPeriodModes => Set<RegistrationPeriodMode>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<DepartmentEmployee> DepartmentEmployees => Set<DepartmentEmployee>();
    public DbSet<DepartmentManager> DepartmentManagers => Set<DepartmentManager>();
    public DbSet<Catagory> Catagories => Set<Catagory>();
    public DbSet<TaskAssignment> TaskAssinments => Set<TaskAssignment>();
    public DbSet<CatagoryReviewer> CatagoryReviewers => Set<CatagoryReviewer>();
    public DbSet<AccessPrivilege> AccessPrivileges => Set<AccessPrivilege>();
    public DbSet<AccessTask> AccessTasks => Set<AccessTask>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<EmployeeId>().HaveConversion<EmployeeIdValueConverter>();
        configurationBuilder.Properties<DepartmentId>().HaveConversion<DepartmentIdValueConverter>();
        configurationBuilder.Properties<CatagoryId>().HaveConversion<CatagoryIdValueConverter>();
        configurationBuilder.Properties<TaskId>().HaveConversion<TaskIdValueConverter>();
        configurationBuilder.Properties<TimeEntryId>().HaveConversion<TimeEntryIdValueConverter>();
        configurationBuilder.Properties<TimeEntryAuditId>().HaveConversion<TimeEntryAuditIdValueConverter>();
        configurationBuilder.Properties<TimeEntryReviewId>().HaveConversion<TimeEntryReviewIdValueConverter>();
        configurationBuilder.Properties<RegistrationPeriodModeId>().HaveConversion<RegistrationPeriodModeIdValueConverter>();
        configurationBuilder.Properties<AccessPrivilegeId>().HaveConversion<AccessPrivilegeIdValueConverter>();
        configurationBuilder.Properties<ExportId>().HaveConversion<ExportIdValueConverter>();
        configurationBuilder.Properties<WorkDate>().HaveConversion<WorkDateValueConverter>();
        configurationBuilder.Properties<WorkDuration>().HaveConversion<WorkDurationValueConverter>();
        configurationBuilder.Properties<WorkDescription>().HaveConversion<WorkDescriptionValueConverter>();
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration in Persistence/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
