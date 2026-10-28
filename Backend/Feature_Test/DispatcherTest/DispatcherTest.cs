using System;
using System.Threading.Tasks;
using Application;
using Xunit;
using Application.Commands; 
using Core.Tools.OperationResult;
namespace UnitTests.Dispatchers;

public class DispatcherTest
{
    // A simple dummy command for testing
    public class DummyCommand { }

    // Mock implementation of ICommandHandler
    public class MockCommandHandler : ICommandHandler<DummyCommand>
    {
        public bool HandlerWasCalled { get; private set; }
        public DummyCommand? ReceivedCommand { get; private set; }
        
        public Task<Result> HandleAsync(DummyCommand command)
        {
            HandlerWasCalled = true;
            ReceivedCommand = command;
            return Task.FromResult(Result.Success());
        }
    }

    // Mock implementation of IServiceProvider to simulate dependency injection behavior
    private class MockServiceProvider : IServiceProvider
    {
        public object? ServiceToReturn { get; set; }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(ICommandHandler<DummyCommand>))
            {
                return ServiceToReturn;
            }
            return null;
        }
    }

    [Fact]
    public async Task DispatchAsync_ShouldCallHandler_WhenHandlerIsRegistered()
    {
        // Arrange
        var commandHandler = new MockCommandHandler();
        var serviceProvider = new MockServiceProvider { ServiceToReturn = commandHandler };
        var dispatcher = new global::Application.Dispatcher(serviceProvider);
        var command = new DummyCommand();

        // Act
        var result = await dispatcher.DispatchAsync(command);

        // Assert
        Assert.True(result.IsSuccess == true);
        Assert.True(commandHandler.HandlerWasCalled);
        Assert.Equal(command, commandHandler.ReceivedCommand);
      
        
    }

    [Fact]
    public async Task DispatchAsync_ShouldThrowInvalidOperationException_WhenHandlerNotFound()
    {
        // Arrange
        var serviceProvider = new MockServiceProvider { ServiceToReturn = null };
        var dispatcher = new global::Application.Dispatcher(serviceProvider);
        var command = new DummyCommand();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(command));
        Assert.Contains("No handler found for command type", ex.Message);
    }
  
    
}

