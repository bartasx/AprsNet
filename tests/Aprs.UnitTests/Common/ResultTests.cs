using Aprs.Domain.Common;
using FluentAssertions;
using Xunit;

namespace Aprs.UnitTests.Common;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        // Note: Cannot access Error on success result - it throws
    }

    [Fact]
    public void Failure_ShouldCreateFailureResult()
    {
        // Arrange
        var error = new Error("Test.Error", "Test error message");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void SuccessWithValue_ShouldCreateSuccessResultWithValue()
    {
        // Arrange
        var value = 42;

        // Act
        var result = Result<int>.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void FailureWithValue_ShouldCreateFailureResultWithDefaultValue()
    {
        // Arrange
        var error = new Error("Test.Error", "Test error message");

        // Act
        var result = Result<int>.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        // Note: Cannot access Value on failure result - it throws
    }

    [Fact]
    public void ImplicitConversion_FromValue_ShouldCreateSuccessResult()
    {
        // Arrange & Act
        Result<string> result = "test value";

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test value");
    }

    [Fact]
    public void Match_OnSuccess_ShouldInvokeSuccessFunc()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var output = result.Match(
            onSuccess: v => $"Value: {v}",
            onFailure: e => $"Error: {e.Message}");

        // Assert
        output.Should().Be("Value: 42");
    }

    [Fact]
    public void Match_OnFailure_ShouldInvokeFailureFunc()
    {
        // Arrange
        var error = new Error("Test.Error", "Something went wrong");
        var result = Result<int>.Failure(error);

        // Act
        var output = result.Match(
            onSuccess: v => $"Value: {v}",
            onFailure: e => $"Error: {e.Message}");

        // Assert
        output.Should().Be("Error: Something went wrong");
    }

    [Fact]
    public void Error_None_ShouldHaveEmptyCodeAndMessage()
    {
        // Assert
        Error.None.Code.Should().BeEmpty();
        Error.None.Message.Should().BeEmpty();
    }

    [Fact]
    public void Error_Validation_ShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var error = Error.Validation("Test validation error");

        // Assert
        error.Code.Should().Be("Validation");
        error.Message.Should().Be("Test validation error");
    }
}
