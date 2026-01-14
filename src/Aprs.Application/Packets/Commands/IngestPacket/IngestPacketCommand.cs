using System.Security.Cryptography;
using System.Text;
using Aprs.Domain.Entities;
using Aprs.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aprs.Application.Packets.Commands.IngestPacket;

public record IngestPacketCommand(AprsPacket Packet) : IRequest;

public class IngestPacketHandler : IRequestHandler<IngestPacketCommand>
{
    private readonly IPacketRepository _repository;
    private readonly Application.Interfaces.ICacheService _cache;
    private readonly ILogger<IngestPacketHandler> _logger;
    private static readonly TimeSpan DeduplicationWindow = TimeSpan.FromSeconds(30);

    public IngestPacketHandler(IPacketRepository repository, Application.Interfaces.ICacheService cache, ILogger<IngestPacketHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task Handle(IngestPacketCommand request, CancellationToken cancellationToken)
    {
        var packet = request.Packet;

        // Use stable hash for deduplication (SHA256 is deterministic across restarts)
        string dedupKey = GenerateDeduplicationKey(packet);

        if (await _cache.ExistsAsync(dedupKey, cancellationToken))
        {
            _logger.LogTrace("Duplicate packet detected for {Sender}", packet.Sender.Value);
            return;
        }

        await _repository.AddAsync(packet, cancellationToken);
        
        // Cache dedup key for configured window
        await _cache.SetAsync(dedupKey, true, DeduplicationWindow, cancellationToken);

        _logger.LogDebug("Ingested packet from {Sender}, type: {Type}", packet.Sender.Value, packet.Type);
    }

    /// <summary>
    /// Generates a stable deduplication key using SHA256 hash.
    /// Key is based on sender callsign and raw content.
    /// </summary>
    private static string GenerateDeduplicationKey(AprsPacket packet)
    {
        var input = $"{packet.Sender.Value}:{packet.RawContent}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var hashString = Convert.ToHexString(hashBytes)[..16]; // First 16 chars (64 bits) is sufficient
        return $"dedup:{hashString}";
    }
}
