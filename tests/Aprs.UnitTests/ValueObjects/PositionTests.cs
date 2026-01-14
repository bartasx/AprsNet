using Aprs.Domain.ValueObjects;
using FluentAssertions;

namespace Aprs.UnitTests.ValueObjects;

public class GeoCoordinateTests
{
    [Fact]
    public void Create_WithValidCoordinates_ShouldSucceed()
    {
        // Act
        var position = GeoCoordinate.Create(52.2297, 21.0122);

        // Assert
        position.Latitude.Should().Be(52.2297);
        position.Longitude.Should().Be(21.0122);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(90, 180)]
    [InlineData(-90, -180)]
    [InlineData(52.2297, 21.0122)]
    [InlineData(-33.8688, 151.2093)] // Sydney
    [InlineData(40.7128, -74.0060)] // New York
    public void Create_WithValidBoundaryCoordinates_ShouldSucceed(double lat, double lon)
    {
        // Act
        var position = GeoCoordinate.Create(lat, lon);

        // Assert
        position.Latitude.Should().Be(lat);
        position.Longitude.Should().Be(lon);
    }

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(100, 0)]
    public void Create_WithInvalidLatitude_ShouldThrow(double lat, double lon)
    {
        // Act & Assert
        var act = () => GeoCoordinate.Create(lat, lon);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*latitude*");
    }

    [Theory]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    [InlineData(0, 200)]
    public void Create_WithInvalidLongitude_ShouldThrow(double lat, double lon)
    {
        // Act & Assert
        var act = () => GeoCoordinate.Create(lat, lon);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*longitude*");
    }

    [Fact]
    public void Equals_WithSameCoordinates_ShouldBeTrue()
    {
        // Arrange
        var pos1 = GeoCoordinate.Create(52.2297, 21.0122);
        var pos2 = GeoCoordinate.Create(52.2297, 21.0122);

        // Act & Assert
        pos1.Should().Be(pos2);
    }

    [Fact]
    public void Equals_WithDifferentCoordinates_ShouldBeFalse()
    {
        // Arrange
        var pos1 = GeoCoordinate.Create(52.2297, 21.0122);
        var pos2 = GeoCoordinate.Create(40.7128, -74.0060);

        // Act & Assert
        pos1.Should().NotBe(pos2);
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var position = GeoCoordinate.Create(52.2297, 21.0122);

        // Act & Assert
        position.ToString().Should().Be("52.2297, 21.0122");
    }
}
