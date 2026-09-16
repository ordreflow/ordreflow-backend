
using Core.Tools.OperationResult;

namespace UnitTests;

public class ErrorTests
{
    [Fact]
    public void Error_ShouldCreateWithCodeAndMessage()
    {
        // Arrange & Act
        var error = new Error("ERR001", "Test error message");
        
        // Assert
        Assert.Equal("ERR001", error.Code);
        Assert.Equal("Test error message", error.Message);
        Assert.Null(error.Type);
    }
    
    [Fact]
    public void Error_ShouldCreateWithCodeMessageAndType()
    {
        // Arrange & Act
        var error = new Error("ERR002", "Validation error", "ValidationError");
        
        // Assert
        Assert.Equal("ERR002", error.Code);
        Assert.Equal("Validation error", error.Message);
        Assert.Equal("ValidationError", error.Type);
    }
}

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Arrange & Act
        var result = Result.Success();
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
    }
    
    [Fact]
    public void Failure_ShouldCreateFailedResultWithError()
    {
        // Arrange
        var error = new Error("ERR001", "Something went wrong");
        
        // Act
        var result = Result.Failure(error);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Equal("ERR001", result.Errors[0].Code);
    }
    
    [Fact]
    public void Failure_ShouldCreateFailedResultWithMultipleErrors()
    {
        // Arrange
        var error1 = new Error("ERR001", "Error 1");
        var error2 = new Error("ERR002", "Error 2");
        
        // Act
        var result = Result.Failure(error1, error2);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(2, result.Errors.Count);
    }
    
    [Fact]
    public void Combine_ShouldReturnSuccessWhenAllResultsSucceed()
    {
        // Arrange
        var result1 = Result.Success();
        var result2 = Result.Success();
        var result3 = Result.Success();
        
        // Act
        var combined = Result.Combine(result1, result2, result3);
        
        // Assert
        Assert.True(combined.IsSuccess);
        Assert.Empty(combined.Errors);
    }
    
    [Fact]
    public void Combine_ShouldReturnFailureWhenAnyResultFails()
    {
        // Arrange
        var result1 = Result.Success();
        var result2 = Result.Failure(new Error("ERR001", "Error 1"));
        var result3 = Result.Failure(new Error("ERR002", "Error 2"));
        
        // Act
        var combined = Result.Combine(result1, result2, result3);
        
        // Assert
        Assert.False(combined.IsSuccess);
        Assert.Equal(2, combined.Errors.Count);
    }
}

public class ResultTTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResultWithValue()
    {
        // Arrange & Act
        var result = Result<int>.Success(42);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Empty(result.Errors);
    }
    
    [Fact]
    public void Success_ShouldCreateSuccessfulResultWithStringValue()
    {
        // Arrange & Act
        var result = Result<string>.Success("Hello World");
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Hello World", result.Value);
    }
    
    [Fact]
    public void Failure_ShouldCreateFailedResultWithError()
    {
        // Arrange
        var error = new Error("ERR001", "Failed to get value");
        
        // Act
        var result = Result<int>.Failure(error);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Equal("ERR001", result.Errors[0].Code);
    }
    
    [Fact]
    public void Value_ShouldThrowExceptionWhenAccessingFailedResult()
    {
        // Arrange
        var error = new Error("ERR001", "Failed");
        var result = Result<int>.Failure(error);
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
    
    [Fact]
    public void ImplicitOperator_ShouldConvertValueToSuccessResult()
    {
        // Arrange & Act
        Result<int> result = 100;
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value);
    }
}