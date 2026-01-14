using System.Reflection;
using Aprs.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aprs.Infrastructure.Persistence;

public class AprsDbContext : DbContext
{
    public DbSet<AprsPacket> Packets => Set<AprsPacket>();

    public AprsDbContext(DbContextOptions<AprsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
