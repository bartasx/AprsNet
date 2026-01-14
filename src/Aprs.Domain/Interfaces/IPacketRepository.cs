using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aprs.Domain.Entities;
using Aprs.Domain.Enums;
using Aprs.Domain.ValueObjects;

namespace Aprs.Domain.Interfaces;

public interface IPacketRepository
{
    Task AddAsync(AprsPacket packet, CancellationToken cancellationToken);
    Task<AprsPacket?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IEnumerable<AprsPacket>> GetBySenderAsync(Callsign sender, int limit, CancellationToken cancellationToken);
    Task<IEnumerable<AprsPacket>> GetLatestAsync(int limit, CancellationToken cancellationToken);
    Task<IEnumerable<AprsPacket>> SearchAsync(string? sender, PacketType? type, DateTime? from, DateTime? to, int limit, CancellationToken cancellationToken);
    
    /// <summary>
    /// Searches packets with pagination support and returns total count.
    /// </summary>
    Task<(IEnumerable<AprsPacket> Packets, int TotalCount)> SearchWithCountAsync(
        string? sender, 
        PacketType? type, 
        DateTime? from, 
        DateTime? to, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken);
}
