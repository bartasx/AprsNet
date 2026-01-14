using System;
using System.Threading;
using System.Threading.Tasks;
using Aprs.Domain.Entities;
using Aprs.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aprs.Sdk;

/// <summary>
/// High-level client for connecting to APRS-IS (Automatic Packet Reporting System - Internet Service).
/// </summary>
/// <remarks>
/// <para>
/// This client provides a simplified interface for connecting to APRS-IS servers,
/// receiving packets, and handling connection lifecycle events.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// using var client = new AprsClient(streamClient, parser, logger);
/// client.PacketReceived += packet => Console.WriteLine($"From: {packet.Sender}");
/// await client.ConnectAsync("N0CALL", "-1", "r/52.23/21.01/50");
/// </code>
/// </para>
/// </remarks>
public class AprsClient : IDisposable
{
    private readonly IAprsStreamClient _streamClient;
    private readonly IPacketParser _parser;
    private readonly ILogger<AprsClient> _logger;

    /// <summary>
    /// Raised when a valid APRS packet is received and successfully parsed.
    /// </summary>
    public event Action<AprsPacket>? PacketReceived;
    
    /// <summary>
    /// Raised when any raw message is received from the server, including unparseable packets.
    /// </summary>
    public event Action<string>? RawMessageReceived;
    
    /// <summary>
    /// Raised when the connection to APRS-IS is established.
    /// </summary>
    public event Action? Connected;
    
    /// <summary>
    /// Raised when the connection to APRS-IS is lost or closed.
    /// </summary>
    public event Action? Disconnected;

    /// <summary>
    /// Gets a value indicating whether the client is currently connected to APRS-IS.
    /// </summary>
    public bool IsConnected => _streamClient.IsConnected;

    /// <summary>
    /// Initializes a new instance of the <see cref="AprsClient"/> class.
    /// </summary>
    /// <param name="streamClient">The underlying stream client for TCP communication.</param>
    /// <param name="parser">The packet parser for converting raw strings to packets.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public AprsClient(IAprsStreamClient streamClient, IPacketParser parser, ILogger<AprsClient> logger)
    {
        _streamClient = streamClient ?? throw new ArgumentNullException(nameof(streamClient));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _streamClient.MessageReceived += OnMessageReceived;
        _streamClient.Disconnected += OnDisconnected;
        _streamClient.Validated += OnValidated;
    }

    /// <summary>
    /// Connects to an APRS-IS server asynchronously.
    /// </summary>
    /// <param name="callsign">Your amateur radio callsign for identification.</param>
    /// <param name="password">
    /// The APRS-IS password for your callsign. Use "-1" for receive-only (unverified) connections.
    /// </param>
    /// <param name="filter">
    /// The APRS-IS filter expression. Examples: "r/52.23/21.01/50" (50km radius),
    /// "b/N0CALL/W1AW" (specific callsigns), "t/poimqstunw" (packet types).
    /// Leave empty for no filtering (all packets).
    /// </param>
    /// <param name="server">The APRS-IS server hostname. Default is "rotate.aprs2.net".</param>
    /// <param name="port">The APRS-IS server port. Default is 14580 (standard filtered port).</param>
    /// <param name="cancellationToken">A token to cancel the connection attempt.</param>
    /// <returns>A task representing the asynchronous connection operation.</returns>
    /// <remarks>
    /// <para>
    /// The default server "rotate.aprs2.net" automatically selects a geographically close server.
    /// </para>
    /// <para>
    /// Port 14580 is the standard filtered port. Other ports include:
    /// 10152 (full feed), 14579 (filtered, no statistics), 23 (test).
    /// </para>
    /// </remarks>
    public async Task ConnectAsync(
        string callsign, 
        string password = "-1", 
        string filter = "", 
        string server = "rotate.aprs2.net", 
        int port = 14580, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Connecting to APRS-IS...");
        await _streamClient.ConnectAsync(server, port, callsign, password, filter, cancellationToken);
        Connected?.Invoke();
    }

    /// <summary>
    /// Disconnects from the APRS-IS server asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous disconnect operation.</returns>
    public async Task DisconnectAsync()
    {
        await _streamClient.DisconnectAsync();
    }

    private void OnMessageReceived(string rawLine)
    {
        RawMessageReceived?.Invoke(rawLine);

        if (PacketReceived != null)
        {
            if (_parser.TryParse(rawLine, out AprsPacket? packet) && packet != null)
            {
                PacketReceived.Invoke(packet);
            }
        }
    }

    private void OnValidated(bool isValid)
    {
        if (isValid)
            _logger.LogInformation("Client validated with server.");
        else
            _logger.LogWarning("Client failed validation (unverified).");
    }

    private void OnDisconnected()
    {
        Disconnected?.Invoke();
    }

    /// <summary>
    /// Releases all resources used by the <see cref="AprsClient"/>.
    /// </summary>
    /// <remarks>
    /// This method disconnects from the server if connected and cleans up event subscriptions.
    /// </remarks>
    public void Dispose()
    {
        _streamClient.DisconnectAsync().GetAwaiter().GetResult();
        _streamClient.MessageReceived -= OnMessageReceived;
        _streamClient.Disconnected -= OnDisconnected;
        _streamClient.Validated -= OnValidated;
        _streamClient.Dispose();
    }
}
