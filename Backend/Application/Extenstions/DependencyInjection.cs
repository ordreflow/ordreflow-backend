using   Core.Tools.OperationResult;

using System.Reflection;
using Domain.Interfaces.IUnitOfWork;
using Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Extenstions;


public static class DependencyInjection
{
   public static IServiceCollection AddApplication(
    this IServiceCollection services)
{
    services.AddScoped<Dispatcher>();

    services.AddScoped<QueryDispatcher>();
    services.AddScoped<IQueryDispatcher, QueryDispatcher>();

    services.AddScoped<TimeRegistrationDomainService>();

    services.AddScoped<ICommandDispatcher>(provider =>
    {
        var innerDispatcher =
            provider.GetRequiredService<Dispatcher>();

        var unitOfWork =
            provider.GetRequiredService<IUnitOfWork>();

        return new UnitOfWorkCommandDispatcherDecorator(
            innerDispatcher,
            unitOfWork);
    });

    // Command handlers
    var assembly = Assembly.GetExecutingAssembly();

    var handlerTypes = assembly.GetTypes()
        .Where(t =>
            t.IsClass &&
            !t.IsAbstract &&
            t.GetInterfaces()
                .Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() ==
                    typeof(ICommandHandler<>)))
        .ToList();

    foreach (var handlerType in handlerTypes)
    {
        var interfaceType = handlerType.GetInterfaces()
            .First(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() ==
                typeof(ICommandHandler<>));

        services.AddScoped(interfaceType, handlerType);
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

