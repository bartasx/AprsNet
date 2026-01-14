using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aprs.Domain.Entities;
using Aprs.Domain.Enums;
using Aprs.Domain.Interfaces;
using Aprs.Domain.ValueObjects;
using Aprs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aprs.Infrastructure.Repositories;

public class PacketRepository : IPacketRepository
{
    private readonly AprsDbContext _context;

    public PacketRepository(AprsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AprsPacket packet, CancellationToken cancellationToken)
    {
        await _context.Packets.AddAsync(packet, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AprsPacket?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Packets.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<AprsPacket>> GetBySenderAsync(Callsign sender, int limit, CancellationToken cancellationToken)
    {
        return await _context.Packets
            .Where(p => p.Sender.Value == sender.Value)
            .OrderByDescending(p => p.ReceivedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AprsPacket>> GetLatestAsync(int limit, CancellationToken cancellationToken)
    {
        return await _context.Packets
            .OrderByDescending(p => p.ReceivedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<AprsPacket>> SearchAsync(string? sender, PacketType? type, DateTime? from, DateTime? to, int limit, CancellationToken cancellationToken)
    {
        var query = BuildSearchQuery(sender, type, from, to);

        return await query
            .OrderByDescending(p => p.ReceivedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<AprsPacket> Packets, int TotalCount)> SearchWithCountAsync(
        string? sender,
        PacketType? type,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = BuildSearchQuery(sender, type, from, to);

        var totalCount = await query.CountAsync(cancellationToken);

        var packets = await query
            .OrderByDescending(p => p.ReceivedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (packets, totalCount);
    }

    private IQueryable<AprsPacket> BuildSearchQuery(string? sender, PacketType? type, DateTime? from, DateTime? to)
    {
        var query = _context.Packets.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(sender))
        {
            query = query.Where(p => p.Sender.Value == sender || p.Sender.BaseCallsign == sender);
        }

        if (type.HasValue)
        {
            query = query.Where(p => p.Type == type.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(p => p.ReceivedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(p => p.ReceivedAt <= to.Value);
        }

        return query;
    }
}
