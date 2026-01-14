using Aprs.Application.Interfaces;
using Aprs.Application.Packets.Commands.IngestPacket;
using Aprs.Domain.Entities;
using Aprs.Domain.Enums;
using Aprs.Domain.Interfaces;
using Aprs.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aprs.UnitTests.Handlers;

public class IngestPacketHandlerTests
{
    private readonly Mock<IPacketRepository> _repositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ILogger<IngestPacketHandler>> _loggerMock;
    private readonly IngestPacketHandler _handler;

    public IngestPacketHandlerTests()
    {
        _repositoryMock = new Mock<IPacketRepository>();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<IngestPacketHandler>>();
        _handler = new IngestPacketHandler(_repositoryMock.Object, _cacheMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_NewPacket_ShouldAddToRepositoryAndCache()
    {
        // Arrange
        var packet = CreateTestPacket();
        var command = new IngestPacketCommand(packet);
        
        _cacheMock.Setup(c => c.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(packet, It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.SetAsync(
            It.Is<string>(s => s.StartsWith("dedup:")),
            It.IsAny<bool>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicatePacket_ShouldNotAddToRepository()
    {
        // Arrange
        var packet = CreateTestPacket();
        var command = new IngestPacketCommand(packet);
        
        _cacheMock.Setup(c => c.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Packet already exists in cache

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<AprsPacket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SamePacketTwice_ShouldGenerateSameDeduplicationKey()
    {
        // Arrange
        var packet1 = CreateTestPacket();
        var packet2 = CreateTestPacket(); // Same content
        
        var capturedKeys = new List<string>();
        
        _cacheMock.Setup(c => c.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((key, _) => capturedKeys.Add(key))
            .ReturnsAsync(false);

        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(new IngestPacketCommand(packet1), CancellationToken.None);
        await _handler.Handle(new IngestPacketCommand(packet2), CancellationToken.None);

        // Assert - both should use the same dedup key pattern
        capturedKeys.Should().HaveCountGreaterThanOrEqualTo(2);
        capturedKeys[0].Should().StartWith("dedup:");
        // Same packet content should generate same dedup key
        capturedKeys[0].Should().Be(capturedKeys[1]);
    }

    [Fact]
    public async Task Handle_DifferentPackets_ShouldGenerateDifferentDeduplicationKeys()
    {
        // Arrange
        var packet1 = CreateTestPacket("N0CALL", "content1");
        var packet2 = CreateTestPacket("W1AW", "content2");
        
        var capturedKeys = new List<string>();
        
        _cacheMock.Setup(c => c.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Callback<string, bool, TimeSpan?, CancellationToken>((key, _, _, _) => capturedKeys.Add(key));

        // Act
        await _handler.Handle(new IngestPacketCommand(packet1), CancellationToken.None);
        await _handler.Handle(new IngestPacketCommand(packet2), CancellationToken.None);

        // Assert
        capturedKeys.Should().HaveCount(2);
        capturedKeys[0].Should().NotBe(capturedKeys[1]);
    }

    [Fact]
    public async Task Handle_ShouldRespectCancellationToken()
    {
        // Arrange
        var packet = CreateTestPacket();
        var command = new IngestPacketCommand(packet);
        var cts = new CancellationTokenSource();
        
        _cacheMock.Setup(c => c.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _handler.Handle(command, cts.Token);

        // Assert
        _cacheMock.Verify(c => c.ExistsAsync(It.IsAny<string>(), cts.Token), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(packet, cts.Token), Times.Once);
    }

    private static AprsPacket CreateTestPacket(string callsign = "N0CALL", string content = "test content")
    {
        return new AprsPacket(
            Callsign.Create(callsign),
            null,
            "APRS,WIDE1-1",
            PacketType.Unknown,
            $"{callsign}>APRS,WIDE1-1:{content}");
    }
}
