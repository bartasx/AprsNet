using Aprs.Application.Packets.Queries.GetPackets;
using FluentAssertions;
using FluentValidation;
using Xunit;

namespace Aprs.UnitTests.Validators;

public class GetPacketsQueryValidatorTests
{
    private readonly GetPacketsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithDefaultValues_ShouldPass()
    {
        // Arrange
        var query = new GetPacketsQuery();

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(1, 100)]
    [InlineData(100, 50)]
    public void Validate_WithValidPageAndPageSize_ShouldPass(int page, int pageSize)
    {
        // Arrange
        var query = new GetPacketsQuery(Page: page, PageSize: pageSize);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithInvalidPage_ShouldFail(int page)
    {
        // Arrange
        var query = new GetPacketsQuery(Page: page);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Validate_WithInvalidPageSize_ShouldFail(int pageSize)
    {
        // Arrange
        var query = new GetPacketsQuery(PageSize: pageSize);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void Validate_WithValidSender_ShouldPass()
    {
        // Arrange
        var query = new GetPacketsQuery(Sender: "N0CALL");

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid!")]    // Invalid characters
    [InlineData("abc")]         // Lowercase not allowed
    [InlineData("N0CALL-ABC")]  // SSID must be numeric
    public void Validate_WithInvalidSender_ShouldFail(string sender)
    {
        // Arrange
        var query = new GetPacketsQuery(Sender: sender);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Sender");
    }
}
