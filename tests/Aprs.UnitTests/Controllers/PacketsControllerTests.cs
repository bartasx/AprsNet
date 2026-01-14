using Aprs.Api.Controllers;
using Aprs.Application.Packets.DTOs;
using Aprs.Application.Packets.Queries.GetPackets;
using Aprs.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Aprs.UnitTests.Controllers;

/// <summary>
/// Unit tests for <see cref="PacketsController"/>.
/// </summary>
public class PacketsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly PacketsController _controller;

    public PacketsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new PacketsController(_mediatorMock.Object);
    }

    [Fact]
    public async Task Get_WithValidQuery_ReturnsOkWithResponse()
    {
        // Arrange
        var query = new GetPacketsQuery(Page: 1, PageSize: 10);
        var expectedResponse = new GetPacketsResponse(
            Items: new List<PacketDto>
            {
                new()
                {
                    Id = 1,
                    Sender = "N0CALL-9",
                    Path = "WIDE1-1",
                    Type = "Position",
                    RawContent = "N0CALL-9>APRS:!5213.78N/02100.73E>test",
                    ReceivedAt = DateTime.UtcNow
                }
            },
            TotalCount: 1,
            Page: 1,
            PageSize: 10,
            TotalPages: 1,
            HasNextPage: false,
            HasPreviousPage: false);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPacketsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Get(query, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GetPacketsResponse>().Subject;
        response.Items.Should().HaveCount(1);
        response.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Get_WithFilters_PassesQueryToMediator()
    {
        // Arrange
        var query = new GetPacketsQuery(
            Sender: "N0CALL",
            Type: PacketType.PositionWithoutTimestamp,
            From: DateTime.UtcNow.AddDays(-1),
            To: DateTime.UtcNow,
            Page: 2,
            PageSize: 25
        );

        GetPacketsQuery? capturedQuery = null;
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPacketsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<GetPacketsResponse>, CancellationToken>((q, _) => capturedQuery = (GetPacketsQuery)q)
            .ReturnsAsync(new GetPacketsResponse(
                Items: Array.Empty<PacketDto>(),
                TotalCount: 0,
                Page: 2,
                PageSize: 25,
                TotalPages: 0,
                HasNextPage: false,
                HasPreviousPage: true));

        // Act
        await _controller.Get(query, CancellationToken.None);

        // Assert
        capturedQuery.Should().NotBeNull();
        capturedQuery!.Sender.Should().Be("N0CALL");
        capturedQuery.Type.Should().Be(PacketType.PositionWithoutTimestamp);
        capturedQuery.Page.Should().Be(2);
        capturedQuery.PageSize.Should().Be(25);
    }

    [Fact]
    public async Task Get_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var query = new GetPacketsQuery(Page: 1, PageSize: 10);
        var emptyResponse = new GetPacketsResponse(
            Items: Array.Empty<PacketDto>(),
            TotalCount: 0,
            Page: 1,
            PageSize: 10,
            TotalPages: 0,
            HasNextPage: false,
            HasPreviousPage: false);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPacketsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyResponse);

        // Act
        var result = await _controller.Get(query, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GetPacketsResponse>().Subject;
        response.Items.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Get_PropagatesCancellationToken()
    {
        // Arrange
        var query = new GetPacketsQuery(Page: 1, PageSize: 10);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        CancellationToken capturedToken = default;
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPacketsQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<GetPacketsResponse>, CancellationToken>((_, ct) => capturedToken = ct)
            .ReturnsAsync(new GetPacketsResponse(
                Items: Array.Empty<PacketDto>(),
                TotalCount: 0,
                Page: 1,
                PageSize: 10,
                TotalPages: 0,
                HasNextPage: false,
                HasPreviousPage: false));

        // Act
        await _controller.Get(query, token);

        // Assert
        capturedToken.Should().Be(token);
    }

    [Fact]
    public async Task Get_WithLargePageSize_ReturnsCorrectPaginationInfo()
    {
        // Arrange
        var query = new GetPacketsQuery(Page: 1, PageSize: 100);
        var response = new GetPacketsResponse(
            Items: Enumerable.Range(1, 100).Select(i => new PacketDto
            {
                Id = i,
                Sender = $"N{i:D4}",
                Path = "WIDE1-1",
                Type = "Position",
                RawContent = $"N{i:D4}>APRS:test",
                ReceivedAt = DateTime.UtcNow
            }).ToList(),
            TotalCount: 500,
            Page: 1,
            PageSize: 100,
            TotalPages: 5,
            HasNextPage: true,
            HasPreviousPage: false);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPacketsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Get(query, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<GetPacketsResponse>().Subject;
        returnedResponse.TotalPages.Should().Be(5);
        returnedResponse.HasNextPage.Should().BeTrue();
        returnedResponse.HasPreviousPage.Should().BeFalse();
    }
}
