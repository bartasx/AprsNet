using System.Text.RegularExpressions;
using Aprs.Domain.Common;

namespace Aprs.Domain.ValueObjects;

/// <summary>
/// Represents a Maidenhead Grid Locator (QTH Locator).
/// Used in amateur radio to specify geographic location.
/// </summary>
public partial class MaidenheadLocator : ValueObject
{
    private static readonly Regex LocatorPattern = LocatorRegex();
    
    /// <summary>
    /// The grid locator value (uppercase).
    /// </summary>
    public string Value { get; }
    
    /// <summary>
    /// Gets the precision level (4, 6, or 8 characters).
    /// </summary>
    public int Precision => Value.Length;

    private MaidenheadLocator(string value)
    {
        Value = value.ToUpperInvariant();
    }

    /// <summary>
    /// Creates a new MaidenheadLocator from a string.
    /// </summary>
    /// <param name="locator">The grid locator string (e.g., "JO91", "JO91wm", "JO91wm48").</param>
    /// <returns>A new MaidenheadLocator instance.</returns>
    /// <exception cref="ArgumentException">Thrown if the locator is invalid.</exception>
    public static MaidenheadLocator Create(string locator)
    {
        if (string.IsNullOrWhiteSpace(locator))
        {
            throw new ArgumentException("Maidenhead locator cannot be null or empty.", nameof(locator));
        }

        if (!IsValid(locator))
        {
            throw new ArgumentException($"Invalid Maidenhead locator format: '{locator}'.", nameof(locator));
        }

        return new MaidenheadLocator(locator);
    }

    /// <summary>
    /// Validates a Maidenhead locator string.
    /// </summary>
    /// <param name="locator">The locator to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool IsValid(string locator)
    {
        if (string.IsNullOrWhiteSpace(locator))
        {
            return false;
        }

        return LocatorPattern.IsMatch(locator);
    }

    /// <summary>
    /// Converts the locator to approximate center coordinates.
    /// </summary>
    /// <returns>A GeoCoordinate representing the center of the grid square.</returns>
    public GeoCoordinate ToCenterPosition()
    {
        double lon = (Value[0] - 'A') * 20 - 180;
        double lat = (Value[1] - 'A') * 10 - 90;

        lon += (Value[2] - '0') * 2;
        lat += (Value[3] - '0');

        // Add half of the square for center
        double lonOffset = 1.0;
        double latOffset = 0.5;

        if (Precision >= 6)
        {
            lon += (char.ToUpperInvariant(Value[4]) - 'A') * (2.0 / 24);
            lat += (char.ToUpperInvariant(Value[5]) - 'A') * (1.0 / 24);
            lonOffset = 1.0 / 24;
            latOffset = 0.5 / 24;
        }

        if (Precision >= 8)
        {
            lon += (Value[6] - '0') * (2.0 / 240);
            lat += (Value[7] - '0') * (1.0 / 240);
            lonOffset = 1.0 / 240;
            latOffset = 0.5 / 240;
        }

        return GeoCoordinate.Create(lat + latOffset, lon + lonOffset);
    }

    /// <summary>
    /// Creates a MaidenheadLocator from coordinates.
    /// </summary>
    /// <param name="latitude">Latitude in degrees.</param>
    /// <param name="longitude">Longitude in degrees.</param>
    /// <param name="precision">Precision level (4, 6, or 8).</param>
    /// <returns>A MaidenheadLocator representing the location.</returns>
    public static MaidenheadLocator FromCoordinates(double latitude, double longitude, int precision = 6)
    {
        if (precision != 4 && precision != 6 && precision != 8)
        {
            throw new ArgumentOutOfRangeException(nameof(precision), "Precision must be 4, 6, or 8.");
        }

        double adjustedLon = longitude + 180;
        double adjustedLat = latitude + 90;

        var chars = new char[precision];

        // Field (20° x 10°)
        chars[0] = (char)('A' + (int)(adjustedLon / 20));
        chars[1] = (char)('A' + (int)(adjustedLat / 10));

        // Square (2° x 1°)
        adjustedLon %= 20;
        adjustedLat %= 10;
        chars[2] = (char)('0' + (int)(adjustedLon / 2));
        chars[3] = (char)('0' + (int)adjustedLat);

        if (precision >= 6)
        {
            // Subsquare (5' x 2.5')
            double subLon = (adjustedLon % 2) * 12;
            double subLat = (adjustedLat % 1) * 24;
            chars[4] = (char)('A' + (int)subLon);
            chars[5] = (char)('A' + (int)subLat);
            adjustedLon = subLon;
            adjustedLat = subLat;
        }

        if (precision >= 8)
        {
            // Extended square
            double extLon = (adjustedLon % 1) * 10;
            double extLat = (adjustedLat % 1) * 10;
            chars[6] = (char)('0' + (int)extLon);
            chars[7] = (char)('0' + (int)extLat);
        }

        return new MaidenheadLocator(new string(chars));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(MaidenheadLocator locator) => locator.Value;

    [GeneratedRegex(@"^[A-Ra-r]{2}[0-9]{2}([A-Xa-x]{2}([0-9]{2})?)?$", RegexOptions.Compiled, matchTimeoutMilliseconds: 100)]
    private static partial Regex LocatorRegex();
}
