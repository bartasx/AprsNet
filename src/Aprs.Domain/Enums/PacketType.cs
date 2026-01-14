namespace Aprs.Domain.Enums;

public enum PacketType
{
    PositionWithoutTimestamp,
    PositionWithTimestamp,
    Message,
    Telemetry,
    Status,
    Object,
    Item,
    Weather,
    MicE,
    Unknown
}
