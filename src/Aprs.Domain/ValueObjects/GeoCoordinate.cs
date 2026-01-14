using System;
using System.Collections.Generic;
using System.Globalization;
using Aprs.Domain.Common;

namespace Aprs.Domain.ValueObjects;

/// <summary>
/// Represents a geographic coordinate with latitude and longitude.
/// This is an immutable value object used throughout the domain.
/// </summary>
/// <remarks>
/// <para>
/// Coordinates are validated during construction to ensure they fall within
/// valid geographic bounds: latitude [-90, 90] and longitude [-180, 180].
/// </para>
/// <para>
/// This class implements the Value Object pattern, meaning two instances
/// with the same latitude and longitude are considered equal.
/// </para>
/// </remarks>
public class GeoCoordinate : ValueObject
{
    /// <summary>
    /// Gets the latitude in decimal degrees.
    /// </summary>
    /// <value>A value between -90 (South Pole) and 90 (North Pole).</value>
    public double Latitude { get; }
    
    /// <summary>
    /// Gets the longitude in decimal degrees.
    /// </summary>
    /// <value>A value between -180 and 180, where negative values are West of the Prime Meridian.</value>
    public double Longitude { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeoCoordinate"/> class.
    /// </summary>
    /// <param name="latitude">The latitude in decimal degrees (-90 to 90).</param>
    /// <param name="longitude">The longitude in decimal degrees (-180 to 180).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when latitude is not between -90 and 90, or longitude is not between -180 and 180.
    /// </exception>
    public GeoCoordinate(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");

        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");

        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>
    /// Creates a new <see cref="GeoCoordinate"/> from latitude and longitude values.
    /// </summary>
    /// <param name="latitude">The latitude in decimal degrees (-90 to 90).</param>
    /// <param name="longitude">The longitude in decimal degrees (-180 to 180).</param>
    /// <returns>A new <see cref="GeoCoordinate"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when coordinates are out of valid range.
    /// </exception>
    public static GeoCoordinate Create(double latitude, double longitude)
    {
        return new GeoCoordinate(latitude, longitude);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
    }

    /// <summary>
    /// Returns a string representation of the coordinate in "latitude, longitude" format.
    /// </summary>
    /// <returns>A string in the format "lat, lon" (e.g., "52.2297, 21.0122").</returns>
    public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0}, {1}", Latitude, Longitude);
}
