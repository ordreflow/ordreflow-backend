using System;
using System.Threading.Tasks;
using Application;
using Xunit;
using Core.Tools.OperationResult;
namespace UnitTests.Dispatchers;

public class QueryDispatcherTest
{
    public class DummyQuery { }

    public class MockQueryHandler : IQueryHandler<DummyQuery, Result<string>>
    {
        public bool HandlerWasCalled { get; private set; }
        public DummyQuery? ReceivedQuery { get; private set; }

        public Task<Result<string>> HandleAsync(DummyQuery query)
        {
            HandlerWasCalled = true;
            ReceivedQuery = query;
            return Task.FromResult(Result<string>.Success("query result"));
        }
    }

    private class MockServiceProvider : IServiceProvider
    {
        public object? ServiceToReturn { get; set; }
        public Type? RequestedType { get; private set; }

        public object? GetService(Type serviceType)
        {
            RequestedType = serviceType;
            return serviceType == typeof(IQueryHandler<DummyQuery, Result<string>>)
                ? ServiceToReturn
                : null;
        }
    }

    [Fact]
    public async Task DispatchAsync_ShouldCallHandler_WhenHandlerIsRegistered()
    {
        // Arrange
        var handler = new MockQueryHandler();
        var serviceProvider = new MockServiceProvider { ServiceToReturn = handler };
        var dispatcher = new QueryDispatcher(serviceProvider);
        var query = new DummyQuery();

        // Act
        var result = await dispatcher.DispatchAsync<DummyQuery, Result<string>>(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(handler.HandlerWasCalled);
        Assert.Same(query, handler.ReceivedQuery);
    }

    [Fact]
    public async Task DispatchAsync_ShouldReturnHandlerResult()
    {
        // Arrange
        var serviceProvider = new MockServiceProvider { ServiceToReturn = new MockQueryHandler() };
        var dispatcher = new QueryDispatcher(serviceProvider);

        // Act
        var result = await dispatcher.DispatchAsync<DummyQuery, Result<string>>(new DummyQuery());

        // Assert
        Assert.Equal("query result", result.Value);
    }

    [Fact]
    public async Task DispatchAsync_ShouldResolveHandlerForQueryAndResultType()
    {
        // Arrange
        var serviceProvider = new MockServiceProvider { ServiceToReturn = new MockQueryHandler() };
        var dispatcher = new QueryDispatcher(serviceProvider);

        // Act
        await dispatcher.DispatchAsync<DummyQuery, Result<string>>(new DummyQuery());

        // Assert
        Assert.Equal(typeof(IQueryHandler<DummyQuery, Result<string>>), serviceProvider.RequestedType);
    }

    [Fact]
    public async Task DispatchAsync_ShouldThrowInvalidOperationException_WhenHandlerNotFound()
    {
        // Arrange
        var serviceProvider = new MockServiceProvider { ServiceToReturn = null };
        var dispatcher = new QueryDispatcher(serviceProvider);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync<DummyQuery, Result<string>>(new DummyQuery()));
        Assert.Contains("No query handler found for", ex.Message);
        Assert.Contains(nameof(DummyQuery), ex.Message);
    }
}
