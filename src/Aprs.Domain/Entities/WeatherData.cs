namespace Aprs.Domain.Entities;

/// <summary>
/// Represents weather data extracted from an APRS weather packet.
/// </summary>
/// <remarks>
/// <para>
/// Weather stations in APRS can report various meteorological measurements.
/// Not all fields are required; stations report what sensors are available.
/// </para>
/// <para>
/// Units follow APRS protocol standards (primarily Imperial/US units).
/// </para>
/// </remarks>
/// <param name="WindDirection">Wind direction in degrees (0-360). Null if not reported.</param>
/// <param name="WindSpeed">Wind speed in miles per hour. Null if not reported.</param>
/// <param name="WindGust">Peak wind gust in miles per hour. Null if not reported.</param>
/// <param name="Temperature">Temperature in degrees Fahrenheit. Null if not reported.</param>
/// <param name="Rain1h">Rain in the last hour in hundredths of an inch. Null if not reported.</param>
/// <param name="Rain24h">Rain in the last 24 hours in hundredths of an inch. Null if not reported.</param>
/// <param name="RainMidnight">Rain since midnight in hundredths of an inch. Null if not reported.</param>
/// <param name="Humidity">Relative humidity percentage (0-100). Value 100 is often encoded as 0. Null if not reported.</param>
/// <param name="Pressure">Barometric pressure in tenths of millibars. Null if not reported.</param>
public record WeatherData(
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
