using Aprs.Domain.ValueObjects;
using FluentAssertions;

namespace Aprs.UnitTests.ValueObjects;

public class CallsignTests
{
    [Theory]
    [InlineData("N0CALL")]
    [InlineData("W1AW")]
    [InlineData("VK2ABC")]
    [InlineData("SP1ABC")]
    [InlineData("JA1XYZ")]
    [InlineData("N0CALL-15")]
    [InlineData("W1AW-9")]
    public void Create_WithValidCallsign_ShouldSucceed(string value)
    {
        // Act
        var callsign = Callsign.Create(value);

        // Assert
        callsign.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrNull_ShouldThrow(string? value)
    {
        // Act & Assert
        var act = () => Callsign.Create(value!);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("TOOLONGCALLSIGN1234567890")]
    public void Create_WithTooLongCallsign_ShouldThrow(string value)
    {
        // Act & Assert
        var act = () => Callsign.Create(value);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equals_WithSameValue_ShouldBeTrue()
    {
        // Arrange
        var callsign1 = Callsign.Create("N0CALL");
        var callsign2 = Callsign.Create("N0CALL");

        // Act & Assert
        callsign1.Should().Be(callsign2);
        (callsign1 == callsign2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValue_ShouldBeFalse()
    {
        // Arrange
        var callsign1 = Callsign.Create("N0CALL");
        var callsign2 = Callsign.Create("W1AW");

        // Act & Assert
        callsign1.Should().NotBe(callsign2);
        (callsign1 != callsign2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var callsign = Callsign.Create("N0CALL");

        // Act & Assert
        callsign.ToString().Should().Be("N0CALL");
    }

    [Fact]
    public void ImplicitConversion_ToString_ShouldWork()
    {
        // Arrange
        var callsign = Callsign.Create("N0CALL");

        // Act
        string value = callsign;

        // Assert
        value.Should().Be("N0CALL");
    }
}
