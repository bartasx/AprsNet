using Aprs.Application.Packets.Queries.GetPackets;
using Aprs.Domain.Entities;
using Aprs.Domain.Enums;
using Aprs.Domain.Interfaces;
using Aprs.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace Aprs.UnitTests.Handlers;

public class GetPacketsHandlerTests
{
    private readonly Mock<IPacketRepository> _repositoryMock;
    private readonly GetPacketsHandler _handler;

    public GetPacketsHandlerTests()
    {
        _repositoryMock = new Mock<IPacketRepository>();
        _handler = new GetPacketsHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithNoFilters_ShouldReturnAllPackets()
    {
        // Arrange
        var packets = CreateTestPackets(5);
        _repositoryMock.Setup(r => r.SearchWithCountAsync(
                null, null, null, null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((packets, 5));

        var query = new GetPacketsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task Handle_WithSenderFilter_ShouldFilterBySender()
    {
        // Arrange
        var packets = CreateTestPackets(2, "N0CALL");
        _repositoryMock.Setup(r => r.SearchWithCountAsync(
                "N0CALL", null, null, null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((packets, 2));

        var query = new GetPacketsQuery(Sender: "N0CALL");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(p => p.Sender.Should().Be("N0CALL"));
    }

    [Fact]
    public async Task Handle_WithPagination_ShouldCalculatePagesCorrectly()
    {
        // Arrange
        var packets = CreateTestPackets(10);
        _repositoryMock.Setup(r => r.SearchWithCountAsync(
                null, null, null, null, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((packets, 25)); // 25 total, page 2 of 3

        var query = new GetPacketsQuery(Page: 2, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FirstPage_ShouldNotHavePreviousPage()
    {
        // Arrange
        var packets = CreateTestPackets(10);
        _repositoryMock.Setup(r => r.SearchWithCountAsync(
                null, null, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((packets, 20));

        var query = new GetPacketsQuery(Page: 1, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_LastPage_ShouldNotHaveNextPage()
    {
        // Arrange
        var packets = CreateTestPackets(5);
        _repositoryMock.Setup(r => r.SearchWithCountAsync(
                null, null, null, null, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((packets, 15)); // 15 total, page 2 of 2

        var query = new GetPacketsQuery(Page: 2, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EmptyResult_ShouldReturnEmptyList()
    {
        // Arrange
        _repositoryMock.Setup(r => r.SearchWithCountAsync(
                It.IsAny<string?>(), It.IsAny<PacketType?>(), It.IsAny<DateTime?>(), 
                It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<AprsPacket>(), 0));

        var query = new GetPacketsQuery(Sender: "NONEXISTENT");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithTypeFilter_ShouldFilterByType()
    {
        // Arrange
        var packets = CreateTestPackets(3);
        _repositoryMock.Setup(r => r.SearchWithCountAsync(
                null, PacketType.PositionWithTimestamp, null, null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((packets, 3));

        var query = new GetPacketsQuery(Type: PacketType.PositionWithTimestamp);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        _repositoryMock.Verify(r => r.SearchWithCountAsync(
            null, PacketType.PositionWithTimestamp, null, null, 1, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDateRange_ShouldFilterByDates()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        var packets = CreateTestPackets(2);
        
        _repositoryMock.Setup(r => r.SearchWithCountAsync(
                null, null, from, to, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((packets, 2));

        var query = new GetPacketsQuery(From: from, To: to);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        _repositoryMock.Verify(r => r.SearchWithCountAsync(
            null, null, from, to, 1, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static IEnumerable<AprsPacket> CreateTestPackets(int count, string callsign = "N0CALL")
    {
        return Enumerable.Range(1, count)
            .Select(i => new AprsPacket(
                Callsign.Create(callsign),
                null,
                "APRS,WIDE1-1",
                PacketType.Unknown,
                $"{callsign}>APRS,WIDE1-1:Test packet {i}"))
            .ToList();
    }
}
