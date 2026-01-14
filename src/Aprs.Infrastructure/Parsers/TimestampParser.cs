using System;
using System.Globalization;

namespace Aprs.Infrastructure.Parsers;

public static class TimestampParser
{
    public static DateTime? Parse(string timestamp, DateTime hint)
    {
        if (string.IsNullOrEmpty(timestamp) || (timestamp.Length != 7 && timestamp.Length != 8))
            return null;

        char indicator = timestamp[timestamp.Length - 1]; // Last char usually (for 7 char), or 6th index?
        // Reference code says timestamp[6] is indicator for 7 char strings.
        
        // Wait, standard formats:
        // DHMz: 092345z (Day 09, 23:45 Zulu) - 7 chars
        // DHM/: 092345/ (Day 09, 23:45 Local) - 7 chars
        // HMS: 234512h (23:45:12 Zulu) - 7 chars
        // MDHM: 10092345 (Month 10, Day 09, 23:45 Zulu) - 8 chars - No indicator char at end usually? Or implicit.
        
        // Let's check length and index 6.
        if (timestamp.Length == 7)
        {
            indicator = timestamp[6];
            if (indicator == 'z' || indicator == '/')
            {
                return DecodeDHM(timestamp, indicator == 'z', hint);
            }
            else if (indicator == 'h')
            {
                return DecodeHMS(timestamp, hint);
            }
        }
        else if (timestamp.Length == 8)
        {
            // MDHM usually valid if all digits
            return DecodeMDHM(timestamp, hint);
        }

        return null;
    }

    private static DateTime DecodeDHM(string raw, bool isZulu, DateTime hint)
    {
        // DDHHMM(z|/)
        int day = int.Parse(raw.Substring(0, 2));
        int hour = int.Parse(raw.Substring(2, 2));
        int minute = int.Parse(raw.Substring(4, 2));

        // Find YYYY/MM based on Day and Hint
        // Assume hint is UtcNow
        DateTime baseTime = hint; // TODO: Adjust for isZulu vs Local if needed, but we store as UTC generally.
        
        int year = baseTime.Year;
        int month = baseTime.Month;

        // If simple logic: if day > current day, it was last month.
        if (day > baseTime.Day + 1) // Tolerance
        {
            month--;
            if (month < 1) { month = 12; year--; }
        }

        // Return DateTime
        return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
    }

    private static DateTime DecodeHMS(string raw, DateTime hint)
    {
        // HHMMSS(h)
        int hour = int.Parse(raw.Substring(0, 2));
        int minute = int.Parse(raw.Substring(2, 2));
        int second = int.Parse(raw.Substring(4, 2));

        // Use Hint Year/Month/Day
        return new DateTime(hint.Year, hint.Month, hint.Day, hour, minute, second, DateTimeKind.Utc);
    }

    private static DateTime DecodeMDHM(string raw, DateTime hint)
    {
        // MMDDHHMM
        int month = int.Parse(raw.Substring(0, 2));
        int day = int.Parse(raw.Substring(2, 2));
        int hour = int.Parse(raw.Substring(4, 2));
        int minute = int.Parse(raw.Substring(6, 2));

        int year = hint.Year;
        // If Month > Current Month, it was last year
        if (month > hint.Month + 1)
        {
            year--;
        }

        return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
    }
}
