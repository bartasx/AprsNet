using Aprs.Infrastructure.Persistence;
using Aprs.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Aprs.IntegrationTests.Repositories;

public class PacketRepositoryTests : IDisposable
{
    private readonly AprsDbContext _context;
    private readonly PacketRepository _repository;

    public PacketRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AprsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AprsDbContext(options);
        _repository = new PacketRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistPacket()
    {
        // Arrange
        var packet = CreateTestPacket();

        // Act
        await _repository.AddAsync(packet, CancellationToken.None);
        var retrieved = await _context.Packets.FirstOrDefaultAsync();

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Sender.Value.Should().Be("N0CALL");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingPacket_ShouldReturnPacket()
    {
        // Arrange
        var packet = CreateTestPacket();
        await _repository.AddAsync(packet, CancellationToken.None);

        // Act
        var retrieved = await _repository.GetByIdAsync(packet.Id, CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(packet.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingPacket_ShouldReturnNull()
    {
        // Act
        var retrieved = await _repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task SearchWithCountAsync_NoFilters_ShouldReturnAllPackets()
    {
        // Arrange
        await SeedPackets(5);

        // Act
        var (packets, count) = await _repository.SearchWithCountAsync(
            null, null, null, null, 1, 100, CancellationToken.None);

        // Assert
        packets.Should().HaveCount(5);
        count.Should().Be(5);
    }

    [Fact]
    public async Task SearchWithCountAsync_WithSenderFilter_ShouldFilterBySender()
    {
        // Arrange
        await _repository.AddAsync(CreateTestPacket("N0CALL"), CancellationToken.None);
        await _repository.AddAsync(CreateTestPacket("N0CALL"), CancellationToken.None);
        await _repository.AddAsync(CreateTestPacket("W1AW"), CancellationToken.None);

        // Act
        var (packets, count) = await _repository.SearchWithCountAsync(
            "N0CALL", null, null, null, 1, 100, CancellationToken.None);

        // Assert
        packets.Should().HaveCount(2);
        count.Should().Be(2);
        packets.Should().AllSatisfy(p => p.Sender.Value.Should().Be("N0CALL"));
    }

    [Fact]
    public async Task SearchWithCountAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        await SeedPackets(25);

        // Act
        var (page1, _) = await _repository.SearchWithCountAsync(
            null, null, null, null, 1, 10, CancellationToken.None);
        var (page2, _) = await _repository.SearchWithCountAsync(
            null, null, null, null, 2, 10, CancellationToken.None);
        var (page3, count) = await _repository.SearchWithCountAsync(
            null, null, null, null, 3, 10, CancellationToken.None);

        // Assert
        page1.Should().HaveCount(10);
        page2.Should().HaveCount(10);
        page3.Should().HaveCount(5);
        count.Should().Be(25);
    }

    [Fact]
    public async Task SearchWithCountAsync_WithDateRange_ShouldFilterByDates()
    {
        // Arrange
        var oldPacket = CreateTestPacket();
        var newPacket = CreateTestPacket();
        
        // Use reflection to set ReceivedAt for testing
        typeof(AprsPacket).GetProperty("ReceivedAt")!
            .SetValue(oldPacket, DateTime.UtcNow.AddDays(-10));
        typeof(AprsPacket).GetProperty("ReceivedAt")!
            .SetValue(newPacket, DateTime.UtcNow);

        await _repository.AddAsync(oldPacket, CancellationToken.None);
        await _repository.AddAsync(newPacket, CancellationToken.None);

        var from = DateTime.UtcNow.AddDays(-5);
        var to = DateTime.UtcNow.AddDays(1);

        // Act
        var (packets, count) = await _repository.SearchWithCountAsync(
            null, null, from, to, 1, 100, CancellationToken.None);

        // Assert
        packets.Should().HaveCount(1);
        count.Should().Be(1);
    }

    private async Task SeedPackets(int count)
    {
        for (int i = 0; i < count; i++)
        {
            await _repository.AddAsync(CreateTestPacket($"CALL{i:D2}"), CancellationToken.None);
        }
    }

    private static AprsPacket CreateTestPacket(string callsign = "N0CALL")
    {
        return AprsPacket.Create(
            Callsign.Create(callsign),
            "APRS,WIDE1-1",
            $"{callsign}>APRS,WIDE1-1:Test packet");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
