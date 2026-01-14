using Aprs.Api.Hubs;
using Aprs.Application.Packets.DTOs;
using Aprs.Domain.Entities;
using Aprs.Application.Packets.Mappings;
using Microsoft.AspNetCore.SignalR;

namespace Aprs.Api.Services;

/// <summary>
/// Service for broadcasting APRS packets to connected SignalR clients.
/// </summary>
/// <remarks>
/// This service is used by the ingestion pipeline to push new packets
/// to real-time subscribers without polling.
/// </remarks>
public interface IPacketBroadcaster
{
    /// <summary>
    /// Broadcasts a packet to all subscribed clients.
    /// </summary>
    /// <param name="packet">The packet to broadcast.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the async operation.</returns>
    Task BroadcastPacketAsync(AprsPacket packet, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of <see cref="IPacketBroadcaster"/> using SignalR.
/// </summary>
public class SignalRPacketBroadcaster : IPacketBroadcaster
{
    private readonly IHubContext<PacketHub> _hubContext;
    private readonly ILogger<SignalRPacketBroadcaster> _logger;
    
    private const string CallsignGroupPrefix = "callsign_";
    private const string AreaGroupPrefix = "area_";

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRPacketBroadcaster"/> class.
    /// </summary>
    /// <param name="hubContext">The SignalR hub context.</param>
    /// <param name="logger">The logger instance.</param>
    public SignalRPacketBroadcaster(
        IHubContext<PacketHub> hubContext,
        ILogger<SignalRPacketBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task BroadcastPacketAsync(AprsPacket packet, CancellationToken cancellationToken = default)
    {
        var dto = packet.ToDto();
        var tasks = new List<Task>();

        // Broadcast to "all packets" group
        tasks.Add(_hubContext.Clients
            .Group(PacketHub.AllPacketsGroup)
            .SendAsync("ReceivePacket", dto, cancellationToken));

        // Broadcast to callsign-specific group
        var callsignGroup = $"{CallsignGroupPrefix}{packet.Sender.Value}";
        tasks.Add(_hubContext.Clients
            .Group(callsignGroup)
            .SendAsync("ReceivePacket", dto, cancellationToken));

        // Broadcast to base callsign group (without SSID)
        if (packet.Sender.Ssid != 0)
        {
            var baseCallsignGroup = $"{CallsignGroupPrefix}{packet.Sender.BaseCallsign}";
            tasks.Add(_hubContext.Clients
                .Group(baseCallsignGroup)
                .SendAsync("ReceivePacket", dto, cancellationToken));
        }

        // Broadcast to area groups if position is available
        if (packet.Position is not null)
        {
            var gridLat = (int)Math.Floor(packet.Position.Latitude);
            var gridLon = (int)Math.Floor(packet.Position.Longitude);
            var areaGroup = $"{AreaGroupPrefix}{gridLat}_{gridLon}";
            
            tasks.Add(_hubContext.Clients
                .Group(areaGroup)
                .SendAsync("ReceivePacket", dto, cancellationToken));
        }

        try
        {
            await Task.WhenAll(tasks);
            _logger.LogDebug(
                "Broadcasted packet from {Sender} to SignalR clients",
                packet.Sender.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to broadcast packet from {Sender} to some SignalR clients",
                packet.Sender.Value);
        }
    }
}
