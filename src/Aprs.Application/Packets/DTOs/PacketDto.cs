namespace Aprs.Application.Packets.DTOs;

/// <summary>
/// Data transfer object representing an APRS packet in API responses.
/// </summary>
/// <remarks>
/// This DTO is used for all packet-related API endpoints. It provides a flat,
/// JSON-friendly representation of the domain's AprsPacket entity.
/// </remarks>
/// <example>
/// <code>
/// {
///   "id": 12345,
///   "sender": "N0CALL-9",
///   "destination": "APRS",
///   "path": "WIDE1-1,WIDE2-2",
///   "type": "Position",
///   "position": { "latitude": 52.2297, "longitude": 21.0122 },
///   "speed": 45.5,
///   "course": 180,
///   "receivedAt": "2024-01-15T10:30:00Z",
///   "rawContent": "N0CALL-9>APRS,WIDE1-1:!5213.78N/02100.73E>...",
///   "comment": "Mobile station"
/// }
/// </code>
/// </example>
public sealed record PacketDto
{
    /// <summary>
    /// Gets the unique identifier for the packet.
    /// </summary>
    public int Id { get; init; }
    
    /// <summary>
    /// Gets the callsign of the station that sent the packet.
    /// </summary>
    /// <example>N0CALL-9</example>
    public required string Sender { get; init; }
    
    /// <summary>
    /// Gets the destination address of the packet.
    /// </summary>
    /// <remarks>
    /// In APRS, this often encodes the software type or device information
    /// rather than being a true destination address.
    /// </remarks>
    public string? Destination { get; init; }
    
    /// <summary>
    /// Gets the digipeater path the packet has traversed.
    /// </summary>
    /// <example>WIDE1-1,WIDE2-2</example>
    public required string Path { get; init; }
    
    /// <summary>
    /// Gets the type of APRS packet.
    /// </summary>
    /// <remarks>
    /// Possible values: Position, Weather, Message, Object, Item, Status, Telemetry, Query, MicE, Unknown.
    /// </remarks>
    public required string Type { get; init; }
    
    /// <summary>
    /// Gets the geographic position from the packet, if available.
    /// </summary>
    public PositionDto? Position { get; init; }
    
    /// <summary>
    /// Gets the speed in knots, if reported.
    /// </summary>
    /// <remarks>
    /// To convert to km/h, multiply by 1.852. To convert to mph, multiply by 1.151.
    /// </remarks>
    public double? Speed { get; init; }
    
    /// <summary>
    /// Gets the course (heading) in degrees (0-360), if reported.
    /// </summary>
    public int? Course { get; init; }
    
    /// <summary>
    /// Gets the weather data from the packet, if this is a weather report.
    /// </summary>
    public WeatherDto? Weather { get; init; }
    
    /// <summary>
    /// Gets the timestamp when the packet was originally sent by the station.
    /// </summary>
    /// <remarks>
    /// This may be null if the packet did not include a timestamp.
    /// </remarks>
    public DateTime? SentTime { get; init; }
    
    /// <summary>
    /// Gets the UTC timestamp when this packet was received by the system.
    /// </summary>
    public DateTime ReceivedAt { get; init; }
    
    /// <summary>
    /// Gets the raw packet content as received from APRS-IS.
    /// </summary>
    /// <remarks>
    /// This is the original, unparsed packet string useful for debugging
    /// or re-parsing with different logic.
    /// </remarks>
    public required string RawContent { get; init; }
    
    /// <summary>
    /// Gets the free-form comment field from the packet.
    /// </summary>
    public string? Comment { get; init; }
    
    /// <summary>
    /// Gets the APRS symbol table identifier ('/' for primary, '\' for alternate).
    /// </summary>
    public string? SymbolTable { get; init; }
    
    /// <summary>
    /// Gets the APRS symbol code character.
    /// </summary>
    /// <remarks>
    /// Combined with SymbolTable, this determines the map icon for the station.
    /// </remarks>
    public string? SymbolCode { get; init; }
}

/// <summary>
/// Data transfer object for geographic position data.
/// </summary>
/// <param name="Latitude">Latitude in decimal degrees (-90 to 90).</param>
/// <param name="Longitude">Longitude in decimal degrees (-180 to 180).</param>
public sealed record PositionDto(double Latitude, double Longitude);

/// <summary>
/// Data transfer object for weather data from APRS weather packets.
/// </summary>
/// <remarks>
/// All values use APRS protocol units (primarily Imperial/US units).
/// </remarks>
/// <param name="WindDirection">Wind direction in degrees (0-360).</param>
/// <param name="WindSpeed">Wind speed in miles per hour.</param>
/// <param name="WindGust">Peak wind gust in miles per hour.</param>
/// <param name="Temperature">Temperature in degrees Fahrenheit.</param>
/// <param name="Rain1h">Rain in the last hour in hundredths of an inch.</param>
/// <param name="Rain24h">Rain in the last 24 hours in hundredths of an inch.</param>
/// <param name="RainMidnight">Rain since midnight in hundredths of an inch.</param>
/// <param name="Humidity">Relative humidity percentage (0-100).</param>
/// <param name="Pressure">Barometric pressure in tenths of millibars.</param>
public sealed record WeatherDto(
    int? WindDirection,
    int? WindSpeed,
    int? WindGust,
    int? Temperature,
    int? Rain1h,
    int? Rain24h,
    int? RainMidnight,
    int? Humidity,
    int? Pressure
);
