using   Core.Tools.OperationResult;

namespace Application;

public class IQueryDispatcher(IServiceProvider serviceProvider) : IQueryHandler<TQuery, TResult>
{
    public Task<Result> DispatchAsync<TCommand>(TCommand command)
    {
        Type serviceType = typeof(ICommandHandler<TCommand>);
        var service = serviceProvider.GetService(serviceType);
        if (service == null)
        {
            throw new InvalidOperationException($"No handler found for command type {typeof(ICommandHandler<TCommand>).FullName}");
            
        }
        ICommandHandler<TCommand> handler = (ICommandHandler<TCommand>)service;
        return handler.HandleAsync(command);
    }
}