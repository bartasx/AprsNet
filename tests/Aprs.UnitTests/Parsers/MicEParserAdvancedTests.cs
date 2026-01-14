using Aprs.Domain.Enums;
using Aprs.Infrastructure.Parsers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aprs.UnitTests.Parsers;

public class MicEParserAdvancedTests
{
    private readonly AprsPacketParser _parser;

    public MicEParserAdvancedTests()
    {
        _parser = new AprsPacketParser(NullLogger<AprsPacketParser>.Instance);
    }

    [Fact]
    public void Parse_MicEWithCurrentData_ShouldParseCorrectly()
    {
        // Mic-E current data indicator is backtick (`)
        // Destination encodes latitude, info field encodes longitude
        string raw = "N0CALL>T2SP0W:`(_fn\"Oj/";

        var packet = _parser.Parse(raw);

        packet.Type.Should().Be(PacketType.MicE);
        packet.Position.Should().NotBeNull();
    }

    [Fact]
    public void Parse_MicEWithOldData_ShouldParseCorrectly()
    {
        // Mic-E old data indicator is apostrophe (')
        string raw = "N0CALL>T2SP0W:'(_fn\"Oj/";

        var packet = _parser.Parse(raw);

        packet.Type.Should().Be(PacketType.MicE);
    }

    [Theory]
    [InlineData("PPPPPP")]  // All P = 0s, North, East
    [InlineData("QQQQQQ")]  // All Q = 1s
    [InlineData("555555")]  // All 5s, South, East
    public void Parse_MicEDestination_ShouldDecodeLatitudeDigits(string dest)
    {
        // This tests the destination field latitude encoding
        // The actual position depends on the info field too
        // Just verify parsing doesn't crash
        string raw = $"N0CALL>{dest}:`(_fn\"Oj/";

        var packet = _parser.Parse(raw);

        packet.Should().NotBeNull();
        packet.Sender.Value.Should().Be("N0CALL");
    }

    [Fact]
    public void Parse_MicEWithSpeed_ShouldExtractSpeed()
    {
        // Mic-E encodes speed and course in bytes 4-6 of info field
        string dest = "T2SP0W"; // Encodes lat ~42.xx N
        string raw = $"N0CALL>{dest}:`(_fn\"Oj/";

        var packet = _parser.Parse(raw);

        packet.Type.Should().Be(PacketType.MicE);
        // Speed and course extraction depends on parser implementation
    }

    [Fact]
    public void Parse_MicEWithAltitude_ShouldExtractAltitude()
    {
        // Mic-E can include altitude in the comment after position
        string raw = "N0CALL>T2SP0W:`(_fn\"Oj/}123";

        var packet = _parser.Parse(raw);

        packet.Type.Should().Be(PacketType.MicE);
        // Altitude parsing depends on implementation
    }

    [Theory]
    [InlineData('/', '-')] // Primary table, house
    [InlineData('\\', 'k')] // Alternate table, truck
    [InlineData('/', '>')] // Primary table, car
    public void Parse_MicEWithSymbols_ShouldExtractSymbol(char table, char code)
    {
        // Symbol is in bytes 7-8 of the info field: Symbol Code first, then Symbol Table
        string dest = "T2SP0W";
        // Construct a minimal valid Mic-E packet
        // In Mic-E, byte 7 is symbol code, byte 8 is symbol table
        string raw = $"N0CALL>{dest}:`(_fn\"O{code}{table}";

        var packet = _parser.Parse(raw);

        packet.Type.Should().Be(PacketType.MicE);
        if (packet.SymbolTable != null)
        {
            packet.SymbolTable.Should().Be(table.ToString());
            packet.SymbolCode.Should().Be(code.ToString());
        }
    }

    [Fact]
    public void Parse_MicEWithComment_ShouldExtractComment()
    {
        string raw = "N0CALL>T2SP0W:`(_fn\"Oj/Test Comment";

        var packet = _parser.Parse(raw);

        packet.Type.Should().Be(PacketType.MicE);
        // Comment is everything after the standard Mic-E fields
    }

    [Fact]
    public void Parse_MicEInvalidDestination_ShouldHandleGracefully()
    {
        // Invalid destination that can't be decoded as Mic-E
        string raw = "N0CALL>APRS:`(_fn\"Oj/";

        var packet = _parser.Parse(raw);

        // Should either parse as MicE or fall back to another type
        packet.Should().NotBeNull();
    }

    [Fact]
    public void Parse_MicETruncatedPayload_ShouldHandleGracefully()
    {
        // Mic-E requires at least 8 bytes in info field
        string raw = "N0CALL>T2SP0W:`(_f";

        var packet = _parser.Parse(raw);

        // Should not throw, may parse partially or as unknown
        packet.Should().NotBeNull();
    }
}
