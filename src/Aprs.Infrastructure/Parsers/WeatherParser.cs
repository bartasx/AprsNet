using System.Text.RegularExpressions;
using Aprs.Domain.Entities;

namespace Aprs.Infrastructure.Parsers;

public static class WeatherParser
{
    // Regex for typical fixed-length weather string components in APRS
    // cDDDsSSSgGGG tTTT rRRR pPPP PPPP hHH bBBBBB
    // keys: 
    // _: Weather report symbol (often starts payload)
    // / or \ or _: Wind Dir (3 chars from start usually if positionless?)
    // But commonly weather is appended to position? e.g. "4903.50N/07201.75W_090/000g000t067..."
    // Positionless: "_10090556c220s004g005t077r000p000P000h50b09900" (Timestamp + Weather)
    
    // We parse the flexible "comment" part usually.
    
    // Keys: 
    // / : Wind Speed (after direction)
    // g : Gust
    // t : Temp
    // r : Rain 1h
    // p : Rain 24h
    // P : Rain mid
    // h : Hum
    // b : Baro
    
    // Note: Wind Dir/Speed usually first 7 chars for positionless: "DDD/SSS"
    // Or "cDDD" "sSSS" in some timestamp formats? 
    // APRS Spec: 
    // with position: "...W_DDD/SSS..." or "...W/DDD/SSS..."
    
    // We will implement a "ParseFields" that scans for key-value pairs in the tail.
    
    public static WeatherData Parse(string payload)
    {
        // Simple regex extraction for standard keys
        int? windGust = ParseInt(payload, "g", 3);
        int? temperature = ParseInt(payload, "t", 3);
        int? rain1h = ParseInt(payload, "r", 3);
        int? rain24h = ParseInt(payload, "p", 3);
        int? rainMidnight = ParseInt(payload, "P", 3);
        int? humidity = ParseInt(payload, "h", 2);
        int? pressure = ParseInt(payload, "b", 5);
        
        int? windDirection = null;
        int? windSpeed = null;

        // Try 'c' prefix for wind direction (positionless weather format: cDDDsSSS)
        windDirection = ParseInt(payload, "c", 3);
        windSpeed = ParseInt(payload, "s", 3);
        
        // If not found, try DDD/SSS pattern (position-based weather)
        if (windDirection == null || windSpeed == null)
        {
            var windMatch = Regex.Match(payload, @"([0-9]{3})/([0-9]{3})");
            if (windMatch.Success)
            {
                if (windDirection == null && int.TryParse(windMatch.Groups[1].Value, out int dir)) 
                    windDirection = dir;
                if (windSpeed == null && int.TryParse(windMatch.Groups[2].Value, out int spd)) 
                    windSpeed = spd;
            }
        }
        
        return new WeatherData(
            windDirection,
            windSpeed,
            windGust,
            temperature,
            rain1h,
            rain24h,
            rainMidnight,
            humidity,
            pressure
        );
    }
    
    private static int? ParseInt(string text, string key, int length)
    {
        // key followed by N digits.
        var match = Regex.Match(text, $"{key}([0-9.]{{{length}}})");
        if (match.Success)
        {
             if (int.TryParse(match.Groups[1].Value, out int val)) return val;
        }
        return null;
    }
}
