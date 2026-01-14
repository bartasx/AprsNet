using System.Diagnostics.Metrics;

namespace Aprs.Infrastructure.Telemetry;

/// <summary>
/// Custom metrics for APRS packet processing monitoring.
/// </summary>
/// <remarks>
/// <para>
/// These metrics are exposed via the Prometheus endpoint at /metrics.
/// </para>
/// <para>
/// Available metrics:
/// <list type="bullet">
///   <item><c>aprs_packets_received_total</c> - Total packets received from APRS-IS</item>
///   <item><c>aprs_packets_parsed_total</c> - Successfully parsed packets</item>
///   <item><c>aprs_packets_parse_errors_total</c> - Packets that failed to parse</item>
///   <item><c>aprs_packets_duplicates_total</c> - Duplicate packets filtered</item>
///   <item><c>aprs_packets_stored_total</c> - Packets stored in database</item>
///   <item><c>aprs_queue_depth</c> - Current depth of ingestion queue</item>
///   <item><c>aprs_signalr_clients</c> - Number of connected SignalR clients</item>
/// </list>
/// </para>
/// </remarks>
public sealed class AprsMetrics
{
    private readonly Counter<long> _packetsReceived;
    private readonly Counter<long> _packetsParsed;
    private readonly Counter<long> _parseErrors;
    private readonly Counter<long> _duplicates;
    private readonly Counter<long> _packetsStored;
    private readonly Histogram<double> _parseLatency;
    private readonly Histogram<double> _storageLatency;
    private readonly UpDownCounter<int> _queueDepth;
    private readonly UpDownCounter<int> _signalRClients;
    private readonly Counter<long> _packetsByType;

    /// <summary>
    /// The meter name used for AprsNet metrics.
    /// </summary>
    public const string MeterName = "AprsNet.Metrics";

    /// <summary>
    /// Initializes a new instance of the <see cref="AprsMetrics"/> class.
    /// </summary>
    /// <param name="meterFactory">The meter factory for creating instruments.</param>
    public AprsMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _packetsReceived = meter.CreateCounter<long>(
            "aprs_packets_received_total",
            unit: "packets",
            description: "Total number of packets received from APRS-IS");

        _packetsParsed = meter.CreateCounter<long>(
            "aprs_packets_parsed_total",
            unit: "packets",
            description: "Total number of successfully parsed packets");

        _parseErrors = meter.CreateCounter<long>(
            "aprs_packets_parse_errors_total",
            unit: "packets",
            description: "Total number of packets that failed to parse");

        _duplicates = meter.CreateCounter<long>(
            "aprs_packets_duplicates_total",
            unit: "packets",
            description: "Total number of duplicate packets filtered out");

        _packetsStored = meter.CreateCounter<long>(
            "aprs_packets_stored_total",
            unit: "packets",
            description: "Total number of packets stored in database");

        _parseLatency = meter.CreateHistogram<double>(
            "aprs_packet_parse_duration_seconds",
            unit: "seconds",
            description: "Time to parse a packet");

        _storageLatency = meter.CreateHistogram<double>(
            "aprs_packet_storage_duration_seconds",
            unit: "seconds",
            description: "Time to store a packet in database");

        _queueDepth = meter.CreateUpDownCounter<int>(
            "aprs_queue_depth",
            unit: "packets",
            description: "Current depth of the ingestion queue");

        _signalRClients = meter.CreateUpDownCounter<int>(
            "aprs_signalr_clients",
            unit: "connections",
            description: "Number of connected SignalR clients");

        _packetsByType = meter.CreateCounter<long>(
            "aprs_packets_by_type_total",
            unit: "packets",
            description: "Packets received by type");
    }

    /// <summary>
    /// Records a packet received from APRS-IS.
    /// </summary>
    public void RecordPacketReceived() => _packetsReceived.Add(1);

    /// <summary>
    /// Records a successfully parsed packet.
    /// </summary>
    /// <param name="packetType">The type of packet parsed.</param>
    public void RecordPacketParsed(string packetType)
    {
        _packetsParsed.Add(1);
        _packetsByType.Add(1, new KeyValuePair<string, object?>("type", packetType));
    }

    /// <summary>
    /// Records a parse error.
    /// </summary>
    public void RecordParseError() => _parseErrors.Add(1);

    /// <summary>
    /// Records a duplicate packet that was filtered.
    /// </summary>
    public void RecordDuplicate() => _duplicates.Add(1);

    /// <summary>
    /// Records a packet stored in the database.
    /// </summary>
    public void RecordPacketStored() => _packetsStored.Add(1);

    /// <summary>
    /// Records the time taken to parse a packet.
    /// </summary>
    /// <param name="seconds">Parse duration in seconds.</param>
    public void RecordParseLatency(double seconds) => _parseLatency.Record(seconds);

    /// <summary>
    /// Records the time taken to store a packet.
    /// </summary>
    /// <param name="seconds">Storage duration in seconds.</param>
    public void RecordStorageLatency(double seconds) => _storageLatency.Record(seconds);

    /// <summary>
    /// Updates the queue depth.
    /// </summary>
    /// <param name="delta">Change in queue depth (positive = added, negative = removed).</param>
    public void UpdateQueueDepth(int delta) => _queueDepth.Add(delta);

    /// <summary>
    /// Records a SignalR client connection.
    /// </summary>
    public void RecordClientConnected() => _signalRClients.Add(1);

    /// <summary>
    /// Records a SignalR client disconnection.
    /// </summary>
    public void RecordClientDisconnected() => _signalRClients.Add(-1);
}
