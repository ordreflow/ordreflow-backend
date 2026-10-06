using   Core.Tools.OperationResult;

using System.Reflection;
using Domain.Interfaces.IUnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Extenstions;


public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        
        
        // 1. Register the underlying real Dispatcher
        services.AddScoped<Dispatcher>();

        // 2. Register the Decorator, passing the real Dispatcher into it as the inner ICommandDispatcher
        services.AddScoped<ICommandDispatcher>(provider =>
        {
            var innerDispatcher = provider.GetRequiredService<Dispatcher>();
            var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
            return new UnitOfWorkCommandDispatcherDecorator(innerDispatcher, unitOfWork);
        });

        // 3. Register all ICommandHandler implementations in this assembly
        var assembly = Assembly.GetExecutingAssembly();
        var handlerTypes = assembly.GetTypes()
            // .Where(t => t.GetInterfaces()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces() // FIX: Added t.IsClass && !t.IsAbstract
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>)))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var interfaceType = handlerType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>));
            
            services.AddScoped(interfaceType, handlerType);
        }

        return services;
    }
    
    
}

