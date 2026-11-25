namespace Persistence;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Domain.Aggregate;
using Domain.Entities;
using Domain.ValueObjects;
using Persistence.Configurations;
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
        configurationBuilder.Properties<EmployeeId>().HaveConversion<EmployeeIdConverter>();
        configurationBuilder.Properties<DepartmentId>().HaveConversion<DepartmentIdConverter>();
        configurationBuilder.Properties<CatagoryId>().HaveConversion<CatagoryIdConverter>();
        configurationBuilder.Properties<TaskId>().HaveConversion<TaskIdConverter>();
        configurationBuilder.Properties<TimeEntryId>().HaveConversion<TimeEntryIdConverter>();
        configurationBuilder.Properties<TimeEntryAuditId>().HaveConversion<TimeEntryAuditIdConverter>();
        configurationBuilder.Properties<TimeEntryReviewId>().HaveConversion<TimeEntryReviewIdConverter>();
        configurationBuilder.Properties<ExportId>().HaveConversion<ExportIdConverter>();
        configurationBuilder.Properties<AccessPrivilegeId>().HaveConversion<AccessPrivilegeIdConverter>();
        configurationBuilder.Properties<RegistrationPeriodModeId>().HaveConversion<RegistrationPeriodModeIdConverter>();
        configurationBuilder.Properties<PersonName>().HaveConversion<PersonNameConverter>();
        configurationBuilder.Properties<EmailAddress>().HaveConversion<EmailAddressConverter>();
        configurationBuilder.Properties<CatagoryName>().HaveConversion<CatagoryNameConverter>();
        configurationBuilder.Properties<TaskName>().HaveConversion<TaskNameConverter>();
        configurationBuilder.Properties<DepartmentName>().HaveConversion<DepartmentNameConverter>();
        configurationBuilder.Properties<TimeEntryDescription>().HaveConversion<TimeEntryDescriptionConverter>();
        configurationBuilder.Properties<RejectionReason>().HaveConversion<RejectionReasonConverter>();
        configurationBuilder.Properties<WorkDate>().HaveConversion<WorkDateConverter>();
        configurationBuilder.Properties<TargetCompletionDate>().HaveConversion<TargetCompletionDateConverter>();
        configurationBuilder.Properties<PlannedDuration>().HaveConversion<PlannedDurationConverter>();
        configurationBuilder.Properties<CutoffDay>().HaveConversion<CutoffDayConverter>();
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration in Persistence/Configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
