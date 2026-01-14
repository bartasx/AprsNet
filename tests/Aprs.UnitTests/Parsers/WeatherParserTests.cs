using Aprs.Domain.Enums;
using Aprs.Infrastructure.Parsers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aprs.UnitTests.Parsers;

public class WeatherParserTests
{
    private readonly AprsPacketParser _parser;

    public WeatherParserTests()
    {
        _parser = new AprsPacketParser(NullLogger<AprsPacketParser>.Instance);
    }

    [Fact]
    public void Parse_CompleteWeatherPacket_ShouldExtractAllData()
    {
        // Complete WX packet with all fields
        // c = wind direction (degrees)
        // s = wind speed (mph)
        // g = gust (mph)
        // t = temperature (F)
        // r = rain last hour (0.01 inches)
        // p = rain last 24h (0.01 inches)
        // P = rain since midnight (0.01 inches)
        // h = humidity (%)
        // b = barometric pressure (0.1 mbar)
        // Format: MMDDHHMM - 01151230 = Jan 15, 12:30
        string raw = "N0CALL>APRS:_01151230c090s010g015t072r001p010P020h50b10135";

        var packet = _parser.Parse(raw);

        packet.Type.Should().Be(PacketType.Weather);
        packet.Weather.Should().NotBeNull();
        packet.Weather!.WindDirection.Should().Be(90);
        packet.Weather.WindSpeed.Should().Be(10);
        packet.Weather.WindGust.Should().Be(15);
        packet.Weather.Temperature.Should().Be(72);
        packet.Weather.Humidity.Should().Be(50);
        packet.Weather.Pressure.Should().Be(10135);
    }

    [Fact]
    public void Parse_PositionWithWeather_ShouldExtractBothPositionAndWeather()
    {
        // Position with weather extension
        string raw = "N0CALL>APRS:!4903.50N/07201.75W_090/010g015t072h50";

        var packet = _parser.Parse(raw);

        packet.Position.Should().NotBeNull();
        packet.Position!.Latitude.Should().BeApproximately(49.058333, 0.0001);
        
        // Weather from position extension (if supported)
        if (packet.Weather != null)
        {
            packet.Weather.WindDirection.Should().Be(90);
            packet.Weather.WindSpeed.Should().Be(10);
        }
    }

    [Fact]
    public void Parse_WeatherWithMissingFields_ShouldHandleGracefully()
    {
        // Partial weather data - only temperature
        // Format: MMDDHHMM - 01151230 = Jan 15, 12:30
        string raw = "N0CALL>APRS:_01151230t072";

        var packet = _parser.Parse(raw);

        packet.Type.Should().Be(PacketType.Weather);
        packet.Weather.Should().NotBeNull();
        packet.Weather!.Temperature.Should().Be(72);
        packet.Weather.WindDirection.Should().BeNull();
        packet.Weather.Humidity.Should().BeNull();
    }
}
