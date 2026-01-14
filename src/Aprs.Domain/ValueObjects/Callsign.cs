using System.Text.RegularExpressions;
using Aprs.Domain.Common;

namespace Aprs.Domain.ValueObjects;

/// <summary>
/// Represents an amateur radio callsign with optional SSID.
/// </summary>
public partial class Callsign : ValueObject
{
    private const int MaxLength = 15; // Base call (6) + hyphen (1) + SSID (2) + some margin
    
    /// <summary>
    /// The full callsign value including SSID.
    /// </summary>
    public string Value { get; }
    
    /// <summary>
    /// The base callsign without SSID.
    /// </summary>
    public string BaseCallsign { get; }
    
    /// <summary>
    /// The SSID (Secondary Station IDentifier), 0-15.
    /// </summary>
    public int Ssid { get; }

    private Callsign(string value, string baseCallsign, int ssid)
    {
        Value = value.ToUpperInvariant();
        BaseCallsign = baseCallsign.ToUpperInvariant();
        Ssid = ssid;
    }

    /// <summary>
    /// Creates a new Callsign from a string.
    /// </summary>
    /// <param name="callsignString">The callsign string (e.g., "N0CALL", "W1AW-9").</param>
    /// <returns>A new Callsign instance.</returns>
    /// <exception cref="ArgumentException">Thrown if the callsign is invalid.</exception>
    public static Callsign Create(string callsignString)
    {
        if (string.IsNullOrWhiteSpace(callsignString))
        {
            throw new ArgumentException("Callsign cannot be empty.", nameof(callsignString));
        }

        if (callsignString.Length > MaxLength)
        {
            throw new ArgumentException($"Callsign is too long (max {MaxLength} characters).", nameof(callsignString));
        }

        var match = CallsignRegex().Match(callsignString);
        if (!match.Success)
        {
            throw new ArgumentException($"Invalid callsign format: {callsignString}", nameof(callsignString));
        }

        string baseCall = callsignString;
        int ssid = 0;

        if (callsignString.Contains('-'))
        {
            var parts = callsignString.Split('-');
            baseCall = parts[0];
            if (parts.Length > 1 && int.TryParse(parts[1], out int parsedSsid))
            {
                if (parsedSsid is < 0 or > 15)
                {
                    throw new ArgumentException($"SSID must be between 0 and 15, got: {parsedSsid}", nameof(callsignString));
                }
                ssid = parsedSsid;
            }
        }

        return new Callsign(callsignString, baseCall, ssid);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override bool Equals(object? obj) => base.Equals(obj);
    public override int GetHashCode() => base.GetHashCode();

    public override string ToString() => Value;

    public static implicit operator string(Callsign callsign) => callsign.Value;

    public static bool operator ==(Callsign? left, Callsign? right) => EqualOperator(left!, right!);
    public static bool operator !=(Callsign? left, Callsign? right) => NotEqualOperator(left!, right!);

    [GeneratedRegex(@"^[A-Z0-9]{2,6}(?:-([0-9]{1,2}))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 100)]
    private static partial Regex CallsignRegex();
}
