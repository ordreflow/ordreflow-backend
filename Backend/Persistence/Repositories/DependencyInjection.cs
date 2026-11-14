using Application.Interfaces;
using Domain.Interfaces;
using Domain.Interfaces.IUnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Identity;
using Persistence.Repositories;

namespace Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
        services.AddScoped<ITopOrdersRepository, TopOrdersRepository>();
        services.AddScoped<IEmployeeAccessRepository, EmployeeAccessRepository>();
        services.AddScoped<IReviewQueueRepository, ReviewQueueRepository>();
        services.AddScoped<IReviewDecisionRepository, ReviewDecisionRepository>();
        services.AddScoped<ITimeRegistrationRepository, TimeRegistrationRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPasswordVerifier, PasswordVerifier>();
        services.AddScoped<IEmployeeAccountProvisioner, IdentityUserAccountProvisioner>();


        return services;
    }
}
