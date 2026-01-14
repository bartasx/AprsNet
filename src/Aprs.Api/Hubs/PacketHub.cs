using Microsoft.AspNetCore.SignalR;

namespace Aprs.Api.Hubs;

/// <summary>
/// SignalR hub for real-time APRS packet streaming.
/// </summary>
/// <remarks>
/// <para>
/// Clients can subscribe to different packet streams:
/// <list type="bullet">
///   <item>All packets (default connection)</item>
///   <item>Specific callsign filter (SubscribeToCallsign)</item>
///   <item>Geographic area filter (SubscribeToArea)</item>
/// </list>
/// </para>
/// <para>
/// Example JavaScript client:
/// <code>
/// const connection = new signalR.HubConnectionBuilder()
///     .withUrl("/hubs/packets")
///     .build();
/// 
/// connection.on("ReceivePacket", (packet) => {
///     console.log("Received:", packet);
/// });
/// 
/// await connection.start();
/// await connection.invoke("SubscribeToCallsign", "N0CALL");
/// </code>
/// </para>
/// </remarks>
public class PacketHub : Hub
{
    private readonly ILogger<PacketHub> _logger;
    
    /// <summary>
    /// Group name prefix for callsign subscriptions.
    /// </summary>
    private const string CallsignGroupPrefix = "callsign_";
    
    /// <summary>
    /// Group name prefix for area subscriptions.
    /// </summary>
    private const string AreaGroupPrefix = "area_";
    
    /// <summary>
    /// Group name for all packets subscription.
    /// </summary>
    public const string AllPacketsGroup = "all_packets";

    /// <summary>
    /// Initializes a new instance of the <see cref="PacketHub"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public PacketHub(ILogger<PacketHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnect, if any.</param>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            exception,
            "Client disconnected: {ConnectionId}",
            Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to all packets stream.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task SubscribeToAll()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, AllPacketsGroup);
        _logger.LogDebug("Client {ConnectionId} subscribed to all packets", Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribe from all packets stream.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    public async Task UnsubscribeFromAll()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, AllPacketsGroup);
        _logger.LogDebug("Client {ConnectionId} unsubscribed from all packets", Context.ConnectionId);
    }

    /// <summary>
    /// Subscribe to packets from a specific callsign.
    /// </summary>
    /// <param name="callsign">The callsign to subscribe to (e.g., "N0CALL" or "N0CALL-9").</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task SubscribeToCallsign(string callsign)
    {
        if (string.IsNullOrWhiteSpace(callsign))
        {
            throw new HubException("Callsign cannot be empty.");
        }

        var groupName = $"{CallsignGroupPrefix}{callsign.ToUpperInvariant()}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug(
            "Client {ConnectionId} subscribed to callsign: {Callsign}",
            Context.ConnectionId,
            callsign);
    }

    /// <summary>
    /// Unsubscribe from a specific callsign.
    /// </summary>
    /// <param name="callsign">The callsign to unsubscribe from.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task UnsubscribeFromCallsign(string callsign)
    {
        if (string.IsNullOrWhiteSpace(callsign))
        {
            throw new HubException("Callsign cannot be empty.");
        }

        var groupName = $"{CallsignGroupPrefix}{callsign.ToUpperInvariant()}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug(
            "Client {ConnectionId} unsubscribed from callsign: {Callsign}",
            Context.ConnectionId,
            callsign);
    }

    /// <summary>
    /// Subscribe to packets within a geographic area.
    /// </summary>
    /// <param name="latitude">Center latitude in decimal degrees.</param>
    /// <param name="longitude">Center longitude in decimal degrees.</param>
    /// <param name="radiusKm">Radius in kilometers.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// Areas are discretized into grid cells for efficient routing.
    /// The actual coverage may be slightly larger than the requested radius.
    /// </remarks>
    public async Task SubscribeToArea(double latitude, double longitude, int radiusKm)
    {
        if (latitude is < -90 or > 90)
        {
            throw new HubException("Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new HubException("Longitude must be between -180 and 180.");
        }

        if (radiusKm is < 1 or > 1000)
        {
            throw new HubException("Radius must be between 1 and 1000 km.");
        }

        // Discretize to 1-degree grid cells for efficient group routing
        var gridLat = (int)Math.Floor(latitude);
        var gridLon = (int)Math.Floor(longitude);
        var groupName = $"{AreaGroupPrefix}{gridLat}_{gridLon}";
        
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug(
            "Client {ConnectionId} subscribed to area: ({Latitude}, {Longitude}) r={Radius}km",
            Context.ConnectionId,
            latitude,
            longitude,
            radiusKm);
    }

    /// <summary>
    /// Unsubscribe from a geographic area.
    /// </summary>
    /// <param name="latitude">Center latitude in decimal degrees.</param>
    /// <param name="longitude">Center longitude in decimal degrees.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task UnsubscribeFromArea(double latitude, double longitude)
    {
        var gridLat = (int)Math.Floor(latitude);
        var gridLon = (int)Math.Floor(longitude);
        var groupName = $"{AreaGroupPrefix}{gridLat}_{gridLon}";
        
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug(
            "Client {ConnectionId} unsubscribed from area: ({Latitude}, {Longitude})",
            Context.ConnectionId,
            latitude,
            longitude);
    }
}
