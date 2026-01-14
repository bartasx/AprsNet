using Aprs.Api.Hubs;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aprs.UnitTests.Hubs;

/// <summary>
/// Unit tests for <see cref="PacketHub"/>.
/// </summary>
public class PacketHubTests
{
    private readonly Mock<ILogger<PacketHub>> _loggerMock;
    private readonly Mock<IGroupManager> _groupsMock;
    private readonly Mock<HubCallerContext> _contextMock;
    private readonly PacketHub _hub;

    public PacketHubTests()
    {
        _loggerMock = new Mock<ILogger<PacketHub>>();
        _groupsMock = new Mock<IGroupManager>();
        _contextMock = new Mock<HubCallerContext>();
        
        _contextMock.Setup(c => c.ConnectionId).Returns("test-connection-id");
        
        _hub = new PacketHub(_loggerMock.Object)
        {
            Groups = _groupsMock.Object,
            Context = _contextMock.Object
        };
    }

    [Fact]
    public async Task SubscribeToAll_AddsClientToAllPacketsGroup()
    {
        // Arrange
        _groupsMock
            .Setup(g => g.AddToGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _hub.SubscribeToAll();

        // Assert
        _groupsMock.Verify(
            g => g.AddToGroupAsync("test-connection-id", PacketHub.AllPacketsGroup, default),
            Times.Once);
    }

    [Fact]
    public async Task UnsubscribeFromAll_RemovesClientFromAllPacketsGroup()
    {
        // Arrange
        _groupsMock
            .Setup(g => g.RemoveFromGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _hub.UnsubscribeFromAll();

        // Assert
        _groupsMock.Verify(
            g => g.RemoveFromGroupAsync("test-connection-id", PacketHub.AllPacketsGroup, default),
            Times.Once);
    }

    [Theory]
    [InlineData("N0CALL")]
    [InlineData("W1AW-9")]
    [InlineData("sp5abc")]
    public async Task SubscribeToCallsign_AddsClientToCallsignGroup(string callsign)
    {
        // Arrange
        _groupsMock
            .Setup(g => g.AddToGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _hub.SubscribeToCallsign(callsign);

        // Assert
        var expectedGroup = $"callsign_{callsign.ToUpperInvariant()}";
        _groupsMock.Verify(
            g => g.AddToGroupAsync("test-connection-id", expectedGroup, default),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task SubscribeToCallsign_WithInvalidCallsign_ThrowsHubException(string? callsign)
    {
        // Act & Assert
        await Assert.ThrowsAsync<HubException>(
            () => _hub.SubscribeToCallsign(callsign!));
    }

    [Theory]
    [InlineData("N0CALL")]
    [InlineData("W1AW-9")]
    public async Task UnsubscribeFromCallsign_RemovesClientFromCallsignGroup(string callsign)
    {
        // Arrange
        _groupsMock
            .Setup(g => g.RemoveFromGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _hub.UnsubscribeFromCallsign(callsign);

        // Assert
        var expectedGroup = $"callsign_{callsign.ToUpperInvariant()}";
        _groupsMock.Verify(
            g => g.RemoveFromGroupAsync("test-connection-id", expectedGroup, default),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task UnsubscribeFromCallsign_WithInvalidCallsign_ThrowsHubException(string? callsign)
    {
        // Act & Assert
        await Assert.ThrowsAsync<HubException>(
            () => _hub.UnsubscribeFromCallsign(callsign!));
    }

    [Fact]
    public async Task SubscribeToArea_AddsClientToAreaGroup()
    {
        // Arrange
        _groupsMock
            .Setup(g => g.AddToGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _hub.SubscribeToArea(52.2297, 21.0122, 50);

        // Assert
        _groupsMock.Verify(
            g => g.AddToGroupAsync("test-connection-id", "area_52_21", default),
            Times.Once);
    }

    [Theory]
    [InlineData(-91, 0, 50)]
    [InlineData(91, 0, 50)]
    public async Task SubscribeToArea_WithInvalidLatitude_ThrowsHubException(
        double latitude, double longitude, int radius)
    {
        // Act & Assert
        await Assert.ThrowsAsync<HubException>(
            () => _hub.SubscribeToArea(latitude, longitude, radius));
    }

    [Theory]
    [InlineData(0, -181, 50)]
    [InlineData(0, 181, 50)]
    public async Task SubscribeToArea_WithInvalidLongitude_ThrowsHubException(
        double latitude, double longitude, int radius)
    {
        // Act & Assert
        await Assert.ThrowsAsync<HubException>(
            () => _hub.SubscribeToArea(latitude, longitude, radius));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 0, -1)]
    [InlineData(0, 0, 1001)]
    public async Task SubscribeToArea_WithInvalidRadius_ThrowsHubException(
        double latitude, double longitude, int radius)
    {
        // Act & Assert
        await Assert.ThrowsAsync<HubException>(
            () => _hub.SubscribeToArea(latitude, longitude, radius));
    }

    [Fact]
    public async Task UnsubscribeFromArea_RemovesClientFromAreaGroup()
    {
        // Arrange
        _groupsMock
            .Setup(g => g.RemoveFromGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _hub.UnsubscribeFromArea(52.2297, 21.0122);

        // Assert
        _groupsMock.Verify(
            g => g.RemoveFromGroupAsync("test-connection-id", "area_52_21", default),
            Times.Once);
    }

    [Theory]
    [InlineData(0.5, 0.5, "area_0_0")]
    [InlineData(-0.5, -0.5, "area_-1_-1")]
    [InlineData(52.9, 21.9, "area_52_21")]
    public async Task SubscribeToArea_UsesCorrectGridDiscretization(
        double latitude, double longitude, string expectedGroup)
    {
        // Arrange
        _groupsMock
            .Setup(g => g.AddToGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _hub.SubscribeToArea(latitude, longitude, 50);

        // Assert
        _groupsMock.Verify(
            g => g.AddToGroupAsync("test-connection-id", expectedGroup, default),
            Times.Once);
    }
}
