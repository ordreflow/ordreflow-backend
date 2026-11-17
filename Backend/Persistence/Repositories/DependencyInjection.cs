using Application.Interfaces;
using Domain.Interfaces;
using Domain.Interfaces.IUnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Identity;
using Persistence.Repositories;
using Persistence.Queries;

namespace Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
        services.AddScoped<IRegistrationPeriodModeRepository, RegistrationPeriodModeRepository>();
        services.AddScoped<ITopCategoriesQuery, TopCategoriesQuery>();
        services.AddScoped<IEmployeeAccessRepository, EmployeeAccessRepository>();
        services.AddScoped<IReviewQueueRepository, ReviewQueueRepository>();
        services.AddScoped<IReviewDecisionRepository, ReviewDecisionRepository>();
        services.AddScoped<ITimeRegistrationRepository, TimeRegistrationRepository>();
        services.AddScoped<IExportRepository, ExportRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IWorkPlanningRepository, WorkPlanningRepository>();
        services.AddScoped<IAccessibleWorkQuery, AccessibleWorkQuery>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICommandTransaction, CommandTransaction>();
        services.AddScoped<IPasswordVerifier, PasswordVerifier>();
        services.AddScoped<IEmployeeAccountProvisioner, IdentityUserAccountProvisioner>();


        return services;
    }
}
