using   Core.Tools.OperationResult;

using System.Reflection;
using Domain.Interfaces.IUnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Application.Exports;
using Application.Handlers;
using Domain.Services;
using Application.Interfaces;

namespace Application.Extenstions;


public static class DependencyInjection
{
   public static IServiceCollection AddApplication(
    this IServiceCollection services)
{
    services.AddScoped<Dispatcher>();

    services.AddScoped<QueryDispatcher>();
    services.AddScoped<IQueryDispatcher, QueryDispatcher>();
    services.AddScoped<TimeEntryReviewCoordinator>();
    services.AddScoped<ExportAccess>();
    services.AddScoped<Application.Accounts.AccountAccess>();
    services.AddScoped<DepartmentStaffingDomainService>();
    services.AddScoped<TimeRegistrationDomainService>();
    services.AddScoped<ExportClosureDomainService>();
    services.AddScoped<TimeEntryReviewDomainService>();

    services.AddScoped<ICommandDispatcher>(provider =>
    {
        var innerDispatcher =
            provider.GetRequiredService<Dispatcher>();

        var unitOfWork =
            provider.GetRequiredService<IUnitOfWork>();

        return new UnitOfWorkCommandDispatcherDecorator(
            innerDispatcher,
            unitOfWork,
            provider.GetRequiredService<ICommandTransaction>());
    });

    // Command handlers
    var assembly = Assembly.GetExecutingAssembly();

    foreach (var handlerType in assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract))
    {
        foreach (var contract in handlerType.GetInterfaces().Where(contract => contract.IsGenericType &&
            (contract.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
             contract.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))))
            services.AddScoped(contract, handlerType);
    }

    // Query handlers
    var queryHandlerTypes = assembly.GetTypes()
        .Where(type => type.IsClass && !type.IsAbstract)
        .SelectMany(type => type.GetInterfaces()
            .Where(@interface =>
                @interface.IsGenericType &&
                @interface.GetGenericTypeDefinition() ==
                typeof(IQueryHandler<,>))
            .Select(@interface => new
            {
                type,
                @interface
            }));

    foreach (var registration in queryHandlerTypes)
    {
        services.AddScoped(
            registration.@interface,
            registration.type);
    }

    return services;
}
    
}
