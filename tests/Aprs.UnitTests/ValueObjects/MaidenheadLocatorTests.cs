using Aprs.Domain.ValueObjects;
using FluentAssertions;

namespace Aprs.UnitTests.ValueObjects;

public class MaidenheadLocatorTests
{
    [Theory]
    [InlineData("JO91")]
    [InlineData("KO02")]
    [InlineData("FN31")]
    [InlineData("JO91wm")]
    [InlineData("KO02lb")]
    [InlineData("FN31pr")]
    [InlineData("JO91wm48")]
    public void Create_WithValidLocator_ShouldSucceed(string locator)
    {
        // Act
        var maidenhead = MaidenheadLocator.Create(locator);

        // Assert
        maidenhead.Value.Should().Be(locator.ToUpperInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrNull_ShouldThrow(string? locator)
    {
        // Act & Assert
        var act = () => MaidenheadLocator.Create(locator!);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("AB")] // Too short
    [InlineData("A")] // Too short
    [InlineData("12AB")] // Wrong format (digits first)
    [InlineData("ABCD12345")] // Too long
    [InlineData("ZZ99")] // Invalid field
    public void Create_WithInvalidLocator_ShouldThrow(string locator)
    {
        // Act & Assert
        var act = () => MaidenheadLocator.Create(locator);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToCenterPosition_ShouldReturnApproximateCenter()
    {
        // Arrange - KO02 is Warsaw area
        var locator = MaidenheadLocator.Create("KO02");

        // Act
        var position = locator.ToCenterPosition();

        // Assert - Should be approximately in Poland
        position.Latitude.Should().BeInRange(51, 53);
        position.Longitude.Should().BeInRange(20, 22);
    }

    [Fact]
    public void Precision_ShouldReturnCorrectLevel()
    {
        // Arrange & Act & Assert
        MaidenheadLocator.Create("JO91").Precision.Should().Be(4);
        MaidenheadLocator.Create("JO91wm").Precision.Should().Be(6);
        MaidenheadLocator.Create("JO91wm48").Precision.Should().Be(8);
    }

    [Fact]
    public void Equals_WithSameValue_ShouldBeTrue()
    {
        // Arrange
        var loc1 = MaidenheadLocator.Create("KO02");
        var loc2 = MaidenheadLocator.Create("ko02"); // Different case

        // Act & Assert
        loc1.Should().Be(loc2);
    }

    [Fact]
    public void ToString_ShouldReturnUppercaseValue()
    {
        // Arrange
        var locator = MaidenheadLocator.Create("ko02lb");

        // Act & Assert
        locator.ToString().Should().Be("KO02LB");
    }
}
