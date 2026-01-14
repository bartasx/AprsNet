using System;
using Aprs.Domain.Entities;

namespace Aprs.Domain.Interfaces;

public interface IPacketParser
{
    AprsPacket Parse(string rawPacket);
    bool TryParse(string rawPacket, out AprsPacket? packet);
}
