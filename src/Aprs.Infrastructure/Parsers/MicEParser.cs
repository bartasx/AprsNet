using System;
using System.Text;
using Aprs.Domain.Entities;
using Aprs.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Aprs.Infrastructure.Parsers;

public class MicEParser
{
    private readonly ILogger _logger;

    public MicEParser(ILogger logger)
    {
        _logger = logger;
    }

    public (GeoCoordinate? Pos, double? Speed, int? Course, string? SymbolTable, string? SymbolCode) Parse(string destinationCallsign, string infoField)
    {
        try
        {
            if (string.IsNullOrEmpty(destinationCallsign) || destinationCallsign.Length != 6)
            {
                // Mic-E destination usually must be 6 chars. If shorter, might be padded or invalid.
                // Spec says fixed 6 chars.
                return (null, null, null, null, null);
            }

            // 1. Decode Destination Address (Latitude, Hemisphere, LongOffset, MessageType)
            DecodeDestination(destinationCallsign, out double latDegrees, out double latMinutes, out int latHemisphere, out int longOffset, out int longHemisphere);

            double latitude = latDegrees + (latMinutes / 60.0);
            if (latHemisphere == -1) latitude *= -1;

            // 2. Decode Info Field (Longitude, Speed, Course, Symbol)
            // Raw format:
            // Byte 1: Longitude Degrees (encoded)
            // Byte 2: Longitude Minutes (encoded)
            // Byte 3: Longitude Hundredths (encoded)
            // Byte 4: Speed/Course
            // Byte 5: Speed/Course
            // Byte 6: Symbol Code
            // Byte 7: Symbol Table
            // ... Altitude/Telemetry

            if (infoField.Length < 8) return (null, null, null, null, null); // Need at least 8 chars for full pos+sym

            // Chars 0, 1, 2 are part of Longitude (Indices 1,2,3 in 1-based or 0,1,2 in 0-based?) 
            // BlueToque says: `var longitudeDegrees = (short)(rawData[1] - 28 + longitudeOffset);`
            // Wait, BlueToque used `rawData` which seemed to include dataType char as index 0?
            // "case DataType.CurrentMicE: packet.ParseMicE(data);"
            // "data" passed to ParseMicE usually HAS the type identifier at [0].
            // Let's assume input `infoField` includes the type identifier char (like '`' or ''') at index 0?
            // "CurrentMicE" data usually starts with '`' (0x60) or ''' (0x27).
            
            // Wait, standard TNC2 payload: `:>Dest:Payload`
            // If Mic-E, Payload starts with the mic-e data.
            // Let's assume infoField starts with the first byte of data (after type identifier? or IS the type identifier?)
            // APRS Spec: "The information field of a Mic-E packet consists of ... 9-byte..."
            // "The data type indicator is NOT part of the Mic-E data... wait."
            // Table 10.1: ` is the Data Type Identifier.
            // So `infoField` passed usually excludes the first char if we striped it in AprsPacketParser?
            // In BlueToque: `data = match.Groups[2].Value;` (Payload) -> `packet.ParseMicE(data);`
            // And inside ParseMicE: `rawData[1]`.
            // So BlueToque expects rawData[0] to be the Type Indicator.
            
            // My AprsPacketParser strips the first char before passing to ParseBody usually.
            // But for Mic-E, the "Type" is tricky.
            // Check AprsPacketParser.cs again. `ParseBody` takes `payload` which INCLUDES the first char.
            // `char dataTypeChar = payload[0];`
            // So if I pass `payload` (full info field) to this function, logic aligns with BlueToque.
            
            string rawData = infoField;
            
            // Longitude
            // d = rawData[1] - 28 + offset
            int longDeg = rawData[1] - 28 + longOffset;
            
            // Adjust 180-189 => -80, 190-199 => -190
            if (longDeg >= 180 && longDeg <= 189) longDeg -= 80;
            else if (longDeg >= 190 && longDeg <= 199) longDeg -= 190;
            
            int longMin = rawData[2] - 28;
            int longHun = rawData[3] - 28;
            
            // Validity Check
             if (longMin >= 60) longMin %= 60; // Spec says "If >= 60, subtract 60" or Mod? BlueToque: % 60.

            double longitude = longDeg + (longMin / 60.0) + (longHun / 100.0 / 60.0); // Wait, BlueToque says: (rawData[3] - 28) * 0.6 ??
            // Minute Hundredths: 0-99. 
            // BlueToque: `(rawData[3] - 28) * 0.6` likely converts 0-99 ints to 'seconds'? No.
            // Spec: "Hundredths of minutes".
            // 0.6 * 100 = 60. Maybe converting to seconds?
            // Position uses Degrees/Minutes/Seconds in BlueToque constructor? `new Longitude(deg, min, sec, ...)`
            // Yes.
            // We use simple Degrees (double).
            // So `longMin / 60.0` is good. `longHun` is hundredths of a minute. `longHun / 100.0` is fraction of minute. `(longHun/100)/60` is degrees? No.
            // Correct: Degrees + (Minutes + Hundredths/100.0) / 60.0
            
            longitude = longDeg + ((longMin + (longHun / 100.0)) / 60.0);
            if (longHemisphere == -1) longitude *= -1;

            // Speed and Course
            // SP = rawData[4]
            // DC = rawData[5] ("Degrees/Course" shared byte? No, D+C)
            // SE = rawData[6] ("Speed/Ten")?
            
            // BlueToque:
            // Speed Byte: rawData[4]
            // Speed/Course Shared: rawData[5]
            // Course: rawData[6]
            
            // speed = (SP - 28) * 10 + (shared - 28) / 10
            int sp = rawData[4] - 28;
            int shared = rawData[5] - 28;
            int dc = rawData[6] - 28;
            
            double speedKnots = (sp * 10) + (shared / 10);
            // Speed is in Knots.
            
            // Course
            // course = ((shared % 10) * 100) + dc
            int course = ((shared % 10) * 100) + dc;
            
            // Symbol
            char symCode = rawData[7];
            char symTable = rawData[8];
            
            return (
                new GeoCoordinate(latitude, longitude), 
                speedKnots, // TODO: Convert to km/h if needed? Usually store raw preference or standard. AprsPacket usually generic.
                course,
                symTable.ToString(),
                symCode.ToString()
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decode Mic-E field: {Info}", infoField);
            return (null, null, null, null, null);
        }
    }

    private void DecodeDestination(string dest, out double degrees, out double minutes, out int latHemi, out int longOffset, out int longHemi)
    {
        // Destination: 6 bytes.
        // A-K: Custom, 0-9: Standard, P-Z: Standard Alt
        // Also L (space?)
        
        // Lat Digit 1, 2, 3, 4, 5, 6
        // Msg identifiers...
        
        // To simplify, we just need Lat + Offsets.
        
        // Digits:
        /*
        Char | Lat Digit | Msg Bit | Custom | North/South | Long Offset | East/West
        0-9  | 0-9       | 0       | 0      |             |             |
        A-J  | 0-9       | 1       | 1      |             |             |
        K    | Space     | 1       | 1      |             |             |
        L    | Space     | 0       | 0      |             |             |
        P-Y  | 0-9       | 1       | 0      |             |             |
        Z    | Space     | 1       | 0      |             |             |
        */
        
        // Index 3 (4th char): N/S.
        // Index 4 (5th): Long Offset 0/100
        // Index 5 (6th): E/W
        
        StringBuilder latStr = new StringBuilder();
        
        latHemi = 1; // North default
        longOffset = 0;
        longHemi = -1; // West default (Wait, BlueToque says 'East' if digit?. Let's check logic)
        // BlueToque: 
        // Index 5: 0-9 => East. P-Z => West.
        
        for (int i = 0; i < 6; i++)
        {
            char c = dest[i];
            int digit = 0;
            
            if (char.IsDigit(c)) digit = c - '0';
            else if (c >= 'A' && c <= 'J') digit = c - 'A';
            else if (c >= 'P' && c <= 'Y') digit = c - 'P';
            else if (c == 'K' || c == 'L' || c == 'Z') { /* Space? treat as 0 or handled? BlueToque appends ' ' */ }

            // Handle Hemisphere/Offset logic based on char type AND index
            if (i == 3)
            {
                // North/South
                if (IsZeroToNine(c) || c == 'L') latHemi = -1; // South
                else latHemi = 1; // North (A-K, P-Z)
            }
            if (i == 4)
            {
                if (IsRange(c, 'P', 'Z')) longOffset = 100;
                else longOffset = 0;
            }
            if (i == 5)
            {
                if (IsRange(c, 'P', 'Z')) longHemi = -1; // West
                else longHemi = 1; // East
            }

            // Append Digit (Assuming standard 2 dig deg, 2 dig min, 2 dig hundredths)
            // DDMM.XX 
            // "Standard MicE format: digits are D D M M h h"
            latStr.Append(digit);
        }
        
        string dStr = latStr.ToString();
        // DD MM hh
        double deg = double.Parse(dStr.Substring(0, 2));
        double min = double.Parse(dStr.Substring(2, 2));
        double hun = double.Parse(dStr.Substring(4, 2));
        
        degrees = deg;
        minutes = min + (hun / 100.0);
    }
    
    private bool IsZeroToNine(char c) => c >= '0' && c <= '9';
    private bool IsRange(char c, char start, char end) => c >= start && c <= end;
}
