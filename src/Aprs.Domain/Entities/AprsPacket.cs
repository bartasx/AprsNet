using System;
using Aprs.Domain.Common;
using Aprs.Domain.Enums;
using Aprs.Domain.ValueObjects;

namespace Aprs.Domain.Entities;

/// <summary>
/// Represents an APRS (Automatic Packet Reporting System) packet.
/// This is the aggregate root for packet-related operations.
/// </summary>
/// <remarks>
/// <para>
/// APRS packets can contain various types of data including:
/// position reports, weather information, telemetry, messages, and more.
/// </para>
/// <para>
/// The packet follows the AX.25 protocol format and is typically received
/// from APRS-IS (Internet Service) or directly from radio TNCs.
/// </para>
/// </remarks>
public class AprsPacket : Entity, IAggregateRoot
{
    /// <summary>
    /// Gets the callsign of the station that sent the packet.
    /// </summary>
    public Callsign Sender { get; private set; } = null!;
    
    /// <summary>
    /// Gets the destination address of the packet.
    /// In APRS, this often encodes additional information like the software type.
    /// </summary>
    public Callsign? Destination { get; private set; }
    
    /// <summary>
    /// Gets the digipeater path the packet has traversed.
    /// Format: "RELAY,WIDE1-1,WIDE2-2" or similar.
    /// </summary>
    public string Path { get; private set; } = null!;
    
    /// <summary>
    /// Gets the type of APRS packet (Position, Weather, Message, etc.).
    /// </summary>
    public PacketType Type { get; private set; }
    
    /// <summary>
    /// Gets the geographic position reported in the packet, if any.
    /// </summary>
    public GeoCoordinate? Position { get; private set; }
    
    /// <summary>
    /// Gets the speed in knots, if reported.
    /// </summary>
    /// <remarks>
    /// Values are sanitized during construction. Speeds exceeding 3500 knots
    /// (approximately Mach 5) are rejected as likely GPS glitches.
    /// </remarks>
    public double? Speed { get; private set; }
    
    /// <summary>
    /// Gets the course (heading) in degrees (0-360), if reported.
    /// </summary>
    public int? Course { get; private set; }
    
    /// <summary>
    /// Gets the weather data contained in the packet, if this is a weather report.
    /// </summary>
    public WeatherData? Weather { get; private set; }
    
    /// <summary>
    /// Gets the timestamp when the packet was originally sent by the station.
    /// </summary>
    /// <remarks>
    /// This may be null if the packet did not include a timestamp.
    /// Not all APRS packets include transmission timestamps.
    /// </remarks>
    public DateTime? SentTime { get; private set; }
    
    /// <summary>
    /// Gets the UTC timestamp when this packet was received by the system.
    /// </summary>
    public DateTime ReceivedAt { get; private set; }
    
    /// <summary>
    /// Gets the raw packet content as received from the APRS-IS feed.
    /// </summary>
    public string RawContent { get; private set; } = null!;
    
    /// <summary>
    /// Gets the free-form comment field from the packet.
    /// </summary>
    public string? Comment { get; private set; }
    
    /// <summary>
    /// Gets the APRS symbol table identifier ('/' for primary, '\' for alternate).
    /// </summary>
    public string? SymbolTable { get; private set; }
    
    /// <summary>
    /// Gets the APRS symbol code character.
    /// </summary>
    /// <remarks>
    /// Combined with <see cref="SymbolTable"/>, this determines the map icon
    /// displayed for the station. For example, "/" + ">" = car icon.
    /// </remarks>
    public string? SymbolCode { get; private set; }

    /// <summary>
    /// Protected constructor for Entity Framework Core.
    /// </summary>
    protected AprsPacket() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AprsPacket"/> class.
    /// </summary>
    /// <param name="sender">The callsign of the sending station. Required.</param>
    /// <param name="destination">The destination address. May contain encoded data.</param>
    /// <param name="path">The digipeater path string.</param>
    /// <param name="type">The type of APRS packet.</param>
    /// <param name="rawContent">The raw packet content. Required.</param>
    /// <param name="position">The geographic position, if available.</param>
    /// <param name="sentTime">The original transmission time, if included.</param>
    /// <param name="comment">The comment field content.</param>
    /// <param name="symbolTable">The symbol table identifier.</param>
    /// <param name="symbolCode">The symbol code character.</param>
    /// <param name="weather">Weather data, for weather packets.</param>
    /// <param name="speed">Speed in knots. Values over 3500 are rejected.</param>
    /// <param name="course">Course in degrees. Values outside 0-360 are rejected.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sender"/> or <paramref name="rawContent"/> is null.
    /// </exception>
    public AprsPacket(
        Callsign sender,
        Callsign? destination,
        string path,
        PacketType type,
        string rawContent,
        GeoCoordinate? position = null,
        DateTime? sentTime = null,
        string? comment = null,
        string? symbolTable = null,
        string? symbolCode = null,
        WeatherData? weather = null,
        double? speed = null,
        int? course = null)
    {
        Sender = sender ?? throw new ArgumentNullException(nameof(sender));
        Destination = destination;
        Path = path;
        Type = type;
        RawContent = rawContent ?? throw new ArgumentNullException(nameof(rawContent));
        Position = position;
        SentTime = sentTime;
        Comment = comment;
        SymbolTable = symbolTable;
        SymbolCode = symbolCode;
        Weather = weather;
        
        // Sanity Checks - filter obvious GPS glitches
        if (speed.HasValue && (speed.Value < 0 || speed.Value > 3500))
            Speed = null;
        else
            Speed = speed;

        if (course.HasValue && (course.Value < 0 || course.Value > 360))
            Course = null;
        else
            Course = course;
        
        ReceivedAt = DateTime.UtcNow;
    }
}
