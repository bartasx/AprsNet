using Aprs.Application.Packets.Commands.IngestPacket;
using Aprs.Domain.Entities;
using Aprs.Domain.Enums;
using Aprs.Domain.ValueObjects;
using FluentAssertions;
using FluentValidation;
using Xunit;

namespace Aprs.UnitTests.Validators;

public class IngestPacketCommandValidatorTests
{
    private readonly IngestPacketCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidPacket_ShouldPass()
    {
        // Arrange
        var packet = new AprsPacket(
            Callsign.Create("N0CALL"),
            null,
            "APRS,WIDE1-1",
            PacketType.PositionWithoutTimestamp,
            "N0CALL>APRS,WIDE1-1:!4903.50N/07201.75W-Test");
        var command = new IngestPacketCommand(packet);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithNullPacket_ShouldFail()
    {
        // Arrange
        var command = new IngestPacketCommand(null!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Packet");
    }
}
