using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ObjectMapper;

namespace WebAPI.Common;

public static class MappingExtensions
{
    public static IServiceCollection AddMappings(this IServiceCollection services)
    {
        services.AddScoped<IMapper, Mapper>();

        var assembly = Assembly.GetExecutingAssembly();
        var configTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMappingConfig<,>)))      
            .ToList();

        foreach (var configType in configTypes)
        {
            var interfaceType = configType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMappingConfig<,>));

            services.AddScoped(interfaceType, configType);
        }

        return services;
    }
}
