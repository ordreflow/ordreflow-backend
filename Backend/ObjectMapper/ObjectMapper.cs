using System;
using System.Text.Json;

namespace ObjectMapper;

public class Mapper : IMapper
{
    private readonly IServiceProvider _serviceProvider;

    public Mapper(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public T Map<T>(object input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));

        var mappingConfigType = typeof(IMappingConfig<,>).MakeGenericType(input.GetType(), typeof(T));
        var mappingConfig = _serviceProvider.GetService(mappingConfigType);

        if (mappingConfig != null)
        {
            var mapMethod = mappingConfigType.GetMethod("Map");
            return (T)mapMethod!.Invoke(mappingConfig, new[] { input })!;
        }

        string tojson = JsonSerializer.Serialize(input);
        var res = JsonSerializer.Deserialize<T>(tojson);
        if (res == null) throw new InvalidOperationException($"Could not map to {typeof(T).Name}");
        return res;
    }
}