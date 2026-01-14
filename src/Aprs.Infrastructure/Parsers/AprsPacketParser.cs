using System;
using System.Text.RegularExpressions;
using Aprs.Domain.Entities;
using Aprs.Domain.Enums;
using Aprs.Domain.Interfaces;
using Aprs.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Aprs.Infrastructure.Parsers;

public class AprsPacketParser : IPacketParser
{
    private readonly ILogger<AprsPacketParser> _logger;
    private readonly MicEParser _micEParser;
    
    // TNC2 Format: CALL>PATH:payload
    private static readonly Regex Tnc2Regex = new(@"^([^>]+)>([^:]+):(.*)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    public AprsPacketParser(ILogger<AprsPacketParser> logger)
    {
        _logger = logger;
        _micEParser = new MicEParser(logger);
    }

    public AprsPacket Parse(string rawPacket)
    {
        if (string.IsNullOrWhiteSpace(rawPacket))
            throw new ArgumentException("Packet cannot be empty", nameof(rawPacket));

        // 1. Clean up
        rawPacket = rawPacket.Trim();

        // 2. Initial Regex for TNC2
        var match = Tnc2Regex.Match(rawPacket);
        if (!match.Success)
        {
            // Fallback or error? For now, throw.
            // TODO: Handle AX.25 raw frames if needed, but usually APRS-IS sends TNC2.
            throw new FormatException($"Invalid APRS packet format: {rawPacket}");
        }

        string senderStr = match.Groups[1].Value;
        string pathAndDestStr = match.Groups[2].Value;
        string payload = match.Groups[3].Value;

        // 3. Extract Destination and Path
        string destStr = pathAndDestStr;
        string pathStr = pathAndDestStr;
        
        int commaIndex = pathAndDestStr.IndexOf(',');
        if (commaIndex > 0)
        {
            destStr = pathAndDestStr.Substring(0, commaIndex);
        }

        var sender = Callsign.Create(senderStr);
        var destination = Callsign.Create(destStr);
        
        // 4. Parse Body (Simplified Strategy for now)
        var (type, pos, sentTime, comment, symTable, symCode, wx, speed, course) = ParseBody(payload, destStr);

        // 5. Build Packet
        return new AprsPacket(
            sender: sender,
            destination: destination,
            path: pathStr,
            type: type,
            rawContent: rawPacket,
            position: pos,
            sentTime: sentTime,
            comment: comment,
            symbolTable: symTable,
            symbolCode: symCode,
            weather: wx,
            speed: speed,
            course: course
        );
    }

    public bool TryParse(string rawPacket, out AprsPacket? packet)
    {
        try
        {
            packet = Parse(rawPacket);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse packet: {RawPacket}", rawPacket);
            packet = null;
            return false;
        }
    }

    private (PacketType Type, GeoCoordinate? Pos, DateTime? SentTime, string? Comment, string? SymTable, string? SymCode, WeatherData? Wx, double? Speed, int? Course) ParseBody(string payload, string destinationCallsign)
    {
        if (string.IsNullOrEmpty(payload))
            return (PacketType.Unknown, null, null, null, null, null, null, null, null);

        char dataTypeChar = payload[0];
        string body = payload.Length > 1 ? payload.Substring(1) : "";

        // Mic-E Detection
        if (dataTypeChar == '`' || dataTypeChar == '\'' || dataTypeChar == 0x1c || dataTypeChar == 0x1d)
        {
             var micE = _micEParser.Parse(destinationCallsign, payload);
             if (micE.Pos != null)
             {
                 return (PacketType.MicE, micE.Pos, null, null, micE.SymbolTable, micE.SymbolCode, null, micE.Speed, micE.Course);
             }
        }

        // Simplified Switch
        switch (dataTypeChar)
        {
            case '!': // Position without timestamp
            case '=': 
                return ParsePosition(body, withTimestamp: false, hasMessaging: dataTypeChar == '=');
            
            case '/': // Position with timestamp
            case '@':
                return ParsePosition(body, withTimestamp: true, hasMessaging: dataTypeChar == '@');

            case ':':
                // Check if it's a message
                return (PacketType.Message, null, null, body, null, null, null, null, null);
                
            case '>':
                return (PacketType.Status, null, null, body, null, null, null, null, null);

            case '[': // Maidenhead Grid Locator Beacon
                // Format: [Grid]Comment
                // Extract grid (e.g. [JO91])
                int closeBracket = body.IndexOf(']');
                if (closeBracket > 0)
                {
                    string grid = body.Substring(0, closeBracket);
                    string comment = body.Substring(closeBracket + 1);
                    var gridPos = MaidenheadParser.Parse(grid);
                    return (PacketType.PositionWithoutTimestamp, gridPos, null, comment, null, null, null, null, null); // Mapped to Position
                }
                return (PacketType.Unknown, null, null, body, null, null, null, null, null);

            case '_': // Weather Report (Positionless)
                // _10090556c...
                // 8 chars timestamp? MDHM.
                if (body.Length > 8)
                {
                     string ts = body.Substring(0, 8);
                     string wxPayload = body.Substring(8);
                     var time = TimestampParser.Parse(ts, DateTime.UtcNow); // Assuming MDHM format usually
                     var wx = WeatherParser.Parse(wxPayload);
                     return (PacketType.Weather, null, time, wxPayload, null, null, wx, null, null);
                }
                return (PacketType.Weather, null, null, body, null, null, null, null, null);

            default:
                // Try to see if it's a raw Mic-E or other format later
                return (PacketType.Unknown, null, null, payload, null, null, null, null, null);
        }
    }

    // Regex for Standard Uncompressed Position
    // Lat: 8 chars (DDMM.hhN), SymTable: 1 char, Long: 9 chars (DDDMM.hhW), SymCode: 1 char
    private static readonly Regex PositionRegex = new Regex(@"^([0-9 \.NS]{8})(.)([0-9 \.EW]{9})(.)(.*)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    // Reserved for Timestamp: 7 chars (DHM/HMS)
    private static readonly Regex TimestampRegex = new Regex(@"^([0-9]{6}[/zh0-9])(.*)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    
    // Cse/Spd extension: 088/036 (Course 088, Speed 036 knots)
    private static readonly Regex SpeedCourseRegex = new Regex(@"^([0-9]{3})/([0-9]{3})(.*)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private (PacketType, GeoCoordinate?, DateTime?, string?, string?, string?, WeatherData?, double?, int?) ParsePosition(string body, bool withTimestamp, bool hasMessaging)
    {
        string remainingBody = body;
        DateTime? sentTime = null;
        
        // 1. Handle Timestamp if present
        if (withTimestamp)
        {
            var timeMatch = TimestampRegex.Match(remainingBody);
            if (timeMatch.Success)
            {
                string tsRaw = timeMatch.Groups[1].Value;
                remainingBody = timeMatch.Groups[2].Value;
                try
                {
                    sentTime = TimestampParser.Parse(tsRaw, DateTime.UtcNow);
                }
                catch
                {
                    // Ignore timestamp parse errors for now, keep packet valid
                }
            }
            else
            {
                // Failed to match timestamp in a timestamped packet
                return (PacketType.Unknown, null, null, body, null, null, null, null, null);
            }
        }

        // 2. Match Position
        var match = PositionRegex.Match(remainingBody);
        if (match.Success)
        {
            string latStr = match.Groups[1].Value;
            string symTable = match.Groups[2].Value;
            string longStr = match.Groups[3].Value;
            string symCode = match.Groups[4].Value;
            string comment = match.Groups[5].Value;
            
            double? speed = null;
            int? course = null;

            // 3. Try Parse Extension (Course/Speed) in comment
            // Format usually immediately follows symbol code? "W/088/036" -> SymCode W, Comment /088/036...
            // Or "W088/036" ?
            // Our regex captures SymCode as group 4. 'comment' is group 5.
            
            var cseMatch = SpeedCourseRegex.Match(comment);
            if (cseMatch.Success)
            {
                if (int.TryParse(cseMatch.Groups[1].Value, out int c)) course = c;
                if (int.TryParse(cseMatch.Groups[2].Value, out int s)) speed = (double)s;
                
                // Optionally update comment to remove the extension data? 
                // Specs specific: "Data Extensions follow the symbol code".
                // We keep full comment for now or strip? Reference implementations usually expose cleaned comment.
                // Let's keep raw comment or maybe stripping is cleaner. 
                // But `comment` variable here IS the rest of the payload.
            }

            try 
            {
                double lat = DecodeLatitude(latStr);
                double lon = DecodeLongitude(longStr);
                
                var pos = new GeoCoordinate(lat, lon);
                var type = sentTime.HasValue ? PacketType.PositionWithTimestamp : PacketType.PositionWithoutTimestamp;
                
                // Attempt Weather Parse in Comment if symbol matches weather?
                // Or mostly generic check:
                WeatherData? wx = null;
                // Common weather symbol is '_' or '/'
                if (symCode == "_" || comment.Contains("g0") || comment.Contains("t0")) // basic heuristics
                {
                    wx = WeatherParser.Parse(comment);
                    if (wx.Temperature.HasValue || wx.WindSpeed.HasValue) type = PacketType.Weather;
                }

                return (type, pos, sentTime, comment, symTable, symCode, wx, speed, course);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decode lat/long: {Lat}/{Long}", latStr, longStr);
                return (PacketType.Unknown, null, null, body, null, null, null, null, null);
            }
        }

        return (PacketType.Unknown, null, null, body, null, null, null, null, null);
    }

    private double DecodeLatitude(string raw)
    {
        // Format: DDMM.hhN
        // 4903.50N
        if (raw.Length != 8) throw new FormatException("Invalid Latitude Length");
        
        char hemisphere = raw[7];
        if (hemisphere != 'N' && hemisphere != 'S') throw new FormatException("Invalid Latitude Hemisphere");

        double degrees = double.Parse(raw.Substring(0, 2), System.Globalization.CultureInfo.InvariantCulture);
        double minutes = double.Parse(raw.Substring(2, 5), System.Globalization.CultureInfo.InvariantCulture); // MM.hh

        double val = degrees + (minutes / 60.0);
        if (hemisphere == 'S') val = -val;
        return Math.Round(val, 6);
    }

    private double DecodeLongitude(string raw)
    {
        // Format: DDDMM.hhW
        // 07201.75W
        if (raw.Length != 9) throw new FormatException("Invalid Longitude Length");

        char hemisphere = raw[8];
        if (hemisphere != 'E' && hemisphere != 'W') throw new FormatException("Invalid Longitude Hemisphere");

        double degrees = double.Parse(raw.Substring(0, 3), System.Globalization.CultureInfo.InvariantCulture);
        double minutes = double.Parse(raw.Substring(3, 5), System.Globalization.CultureInfo.InvariantCulture); // MM.hh

        double val = degrees + (minutes / 60.0);
        if (hemisphere == 'W') val = -val;
        return Math.Round(val, 6);
    }
}
