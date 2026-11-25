
using   Core.Tools.OperationResult;

namespace Application;

public class Dispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    public Task<Result<TValue>> DispatchAsync<TCommand, TValue>(TCommand command, CancellationToken cancellationToken = default)
    {
        var handler = serviceProvider.GetService(typeof(ICommandHandler<TCommand, TValue>)) as ICommandHandler<TCommand, TValue>
            ?? throw new InvalidOperationException($"No result-returning handler found for {typeof(TCommand).FullName}");
        return handler.HandleAsync(command, cancellationToken);
    }

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
