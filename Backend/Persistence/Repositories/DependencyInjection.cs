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

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPasswordVerifier, PasswordVerifier>();
        services.AddScoped<IUserAccountProvisioner, IdentityUserAccountProvisioner>();


        return services;
    }
}