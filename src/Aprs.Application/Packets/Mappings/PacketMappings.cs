using Aprs.Application.Packets.DTOs;
using Aprs.Domain.Entities;

namespace Aprs.Application.Packets.Mappings;

/// <summary>
/// Extension methods for mapping domain entities to DTOs.
/// </summary>
public static class PacketMappings
{
    /// <summary>
    /// Maps an <see cref="AprsPacket"/> to a <see cref="PacketDto"/>.
    /// </summary>
    public static PacketDto ToDto(this AprsPacket packet)
    {
        return new PacketDto
        {
            Id = packet.Id,
            Sender = packet.Sender.Value,
            Destination = packet.Destination?.Value,
            Path = packet.Path,
            Type = packet.Type.ToString(),
            Position = packet.Position != null
                ? new PositionDto(packet.Position.Latitude, packet.Position.Longitude)
                : null,
            Speed = packet.Speed,
            Course = packet.Course,
            Weather = packet.Weather != null
                ? new WeatherDto(
                    packet.Weather.WindDirection,
                    packet.Weather.WindSpeed,
                    packet.Weather.WindGust,
                    packet.Weather.Temperature,
                    packet.Weather.Rain1h,
                    packet.Weather.Rain24h,
                    packet.Weather.RainMidnight,
                    packet.Weather.Humidity,
                    packet.Weather.Pressure)
                : null,
            SentTime = packet.SentTime,
            ReceivedAt = packet.ReceivedAt,
            RawContent = packet.RawContent,
            Comment = packet.Comment,
            SymbolTable = packet.SymbolTable,
            SymbolCode = packet.SymbolCode
        };
    }

    /// <summary>
    /// Maps a collection of <see cref="AprsPacket"/> to <see cref="PacketDto"/>.
    /// </summary>
    public static IEnumerable<PacketDto> ToDto(this IEnumerable<AprsPacket> packets)
    {
        return packets.Select(p => p.ToDto());
    }
}
