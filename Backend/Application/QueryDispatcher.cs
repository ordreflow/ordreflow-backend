namespace Application;

public sealed class QueryDispatcher(
    IServiceProvider serviceProvider)
    : IQueryDispatcher
{
    public Task<TResult> DispatchAsync<TQuery, TResult>(
        TQuery query)
    {
        var serviceType =
            typeof(IQueryHandler<,>)
                .MakeGenericType(
                    typeof(TQuery),
                    typeof(TResult));

        var service = serviceProvider.GetService(serviceType);

        if (service is null)
        {
            throw new InvalidOperationException(
                $"No query handler found for {typeof(TQuery).FullName}");
        }

        var handler =
            (IQueryHandler<TQuery, TResult>)service;

        return handler.HandleAsync(query);
    }
}