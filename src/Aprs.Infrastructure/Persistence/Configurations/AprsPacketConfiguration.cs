using Aprs.Domain.Entities;
using Aprs.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aprs.Infrastructure.Persistence.Configurations;

public class AprsPacketConfiguration : IEntityTypeConfiguration<AprsPacket>
{
    public void Configure(EntityTypeBuilder<AprsPacket> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Ignore(p => p.DomainEvents);

        // Value Objects Configuration
        
        // Sender (Callsign)
        builder.OwnsOne(p => p.Sender, sender =>
        {
            sender.Property(c => c.Value).HasColumnName("sender_callsign").IsRequired().HasMaxLength(15);
            sender.Property(c => c.BaseCallsign).HasColumnName("sender_base").IsRequired().HasMaxLength(10);
            sender.Property(c => c.Ssid).HasColumnName("sender_ssid").IsRequired();
            sender.HasIndex(c => c.Value);
        });

        // Destination (Callsign) -> Optional
        builder.OwnsOne(p => p.Destination, dest =>
        {
            dest.Property(c => c.Value).HasColumnName("dest_callsign").HasMaxLength(15);
            dest.Property(c => c.BaseCallsign).HasColumnName("dest_base").HasMaxLength(10);
            dest.Property(c => c.Ssid).HasColumnName("dest_ssid");
        });

        // Position (GeoCoordinate) -> Optional
        builder.OwnsOne(p => p.Position, pos =>
        {
            pos.Property(g => g.Latitude).HasColumnName("latitude");
            pos.Property(g => g.Longitude).HasColumnName("longitude");
            
            // Index for geospatial queries? Standard B-Tree for now.
            // PostGIS would be better but requires Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite
            // For now, simple columns.
            // For now, simple columns.
            pos.HasIndex(g => g.Latitude);
            pos.HasIndex(g => g.Longitude);
        });

        // Weather Data (Owned)
        builder.OwnsOne(p => p.Weather, wx =>
        {
            wx.Property(w => w.WindDirection).HasColumnName("wx_wind_dir");
            wx.Property(w => w.WindSpeed).HasColumnName("wx_wind_speed");
            wx.Property(w => w.WindGust).HasColumnName("wx_wind_gust");
            wx.Property(w => w.Temperature).HasColumnName("wx_temp");
            wx.Property(w => w.Rain1h).HasColumnName("wx_rain_1h");
            wx.Property(w => w.Rain24h).HasColumnName("wx_rain_24h");
            wx.Property(w => w.RainMidnight).HasColumnName("wx_rain_midnight");
            wx.Property(w => w.Humidity).HasColumnName("wx_humidity");
            wx.Property(w => w.Pressure).HasColumnName("wx_pressure");
        });

        builder.Property(p => p.ReceivedAt).IsRequired();
        builder.HasIndex(p => p.ReceivedAt).IsDescending();
        
        builder.Property(p => p.SentTime); // Optional

        builder.Property(p => p.RawContent).IsRequired();
        builder.Property(p => p.Path).HasMaxLength(100);
        
        builder.Property(p => p.Type).HasConversion<string>(); // Use Enum string for readability
        builder.HasIndex(p => p.Type);
    }
}
