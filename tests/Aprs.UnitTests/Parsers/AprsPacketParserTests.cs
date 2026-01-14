using System;
using Aprs.Domain.Enums;
using Aprs.Infrastructure.Parsers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aprs.UnitTests.Parsers;

public class AprsPacketParserTests
{
    private readonly AprsPacketParser _parser;

    public AprsPacketParserTests()
    {
        _parser = new AprsPacketParser(NullLogger<AprsPacketParser>.Instance);
    }

    [Fact]
    public void Parse_ShouldParse_StandardPosition_NoTimestamp()
    {
        // Example: N0CALL>APRS,WIDE1-1:!4903.50N/07201.75W-Test Packet
        string raw = "N0CALL>APRS,WIDE1-1:!4903.50N/07201.75W-Test Packet";

        var packet = _parser.Parse(raw);

        packet.Should().NotBeNull();
        packet.Sender.Value.Should().Be("N0CALL");
        packet.Type.Should().Be(PacketType.PositionWithoutTimestamp);
        packet.Position.Should().NotBeNull();
        packet.Position!.Latitude.Should().Be(49.058333); // 49 deg 03.50 min = 49 + 3.5/60
        packet.Position!.Longitude.Should().Be(-72.029167); // 72 deg 01.75 min W = -(72 + 1.75/60)
        packet.Comment.Should().Be("Test Packet");
        packet.SymbolTable.Should().Be("/");
        packet.SymbolCode.Should().Be("-");
    }

    [Fact]
    public void Parse_ShouldParse_StandardPosition_WithTimestamp_DHM()
    {
        // Example: N0CALL>APRS:/092345z4903.50N/07201.75W-Test
        // 09th day, 23:45 UTC
        string raw = "N0CALL>APRS:/092345z4903.50N/07201.75W-Test";

        var packet = _parser.Parse(raw);

        packet.Type.Should().Be(PacketType.PositionWithTimestamp);
        packet.SentTime.Should().NotBeNull();
        packet.SentTime!.Value.Day.Should().Be(9);
        packet.SentTime!.Value.Hour.Should().Be(23);
        packet.SentTime!.Value.Minute.Should().Be(45);
        packet.Position!.Latitude.Should().BeApproximately(49.058333, 0.0001);
    }

    [Fact]
    public void Parse_ShouldParse_MicE_Packet()
    {
        // Need a valid Mic-E String.
        // Destination: encode Lat. Info: encode Long.
        // Let's verify with values.
        // Lat: 49 deg 03.50 N. 
        // 4 -> '4' (Msg=0, Cust=0, N/S=N, E/W=E).
        // Check MicEParser tables.
        // 0-9: Lat digit, Msg=0, Cust=0.
        // Dest: "490350" (if all standard) -> 49deg 03.50min.
        // Ind 3 checked for N/S. '3'. 0-9 is South?
        // Wait, MicEParser:
        // if (i == 3) if (IsZeroToNine(c) || c == 'L') latHemi = -1; // South
        // So '3' at index 3 means South.
        // To get North, we need P-Z at index 3. 
        // 3 + 'P' - A? No.
        // P-Z encodes digits 0-9 but Msg=1, Custom=0.
        // 'S' is P(0), Q(1), R(2), S(3). 
        // So 'S' at index 3 encodes Digit 3 and North.
        
        // Dest: "490S50"
        // 4,9,0 standard. S (North, Digit 3). 5 (Standard, East?), 0 (Standard East).
        // MicEParser:
        // i=5 (last char). P-Z is West. 0-9/A-K/L is East.
        // So '0' is East.
        // To be West (like -72 ...), we need last char "W"? No, digit mapped to P-Z.
        // 0 -> 'P'.
        
        // Dest: "490S5P" -> 49deg 03.50min N, LongOffset 0, West.
        
        // Info:
        // Type: ` (CurrentMicE)
        // Payload: `...
        
        // Longitude: 72deg 01.75min.
        // d = raw[1] - 28 + offset.
        // 72 = x - 28 + 0 => x = 100. 'd' char ascii 100.
        // m = raw[2] - 28. 1 = x - 28 => x = 29. ASCII 29?
        // Wait, 28 is lower bound. 
        // 1 = x - 28 -> x = 29. 
        // h = raw[3] - 28. 75 = x - 28 -> x = 103. 'g'
        // SP, DC, SE...
        
        // Let's construct a raw string carefully or treat "Parse_ShouldHandle_MicE" with a known sample.
        // Sample from online or reference?
        // "T7R35P" (Dest)
        // T=4 (+Msg1, Cust0). 7=7. R=2 (N). 3=3 (N). 5=5 (E). P=0 (W).
        // 47deg 23.50min N, West.
        
        // Let's assume input: "N0CALL>T7R35P:`...encoded..."
        // I won't construct complex binary payload here by hand easily without risks.
        // But I can test with a mocked minimal string if I trust the parser math.
        
        // Let's construct a minimal valid one.
        // Dest: "111111" -> 11deg 11.11min South, East.
        // Payload: "`" + (char)(72+28) + (char)(1+28) + (char)(75+28) + (char)(28) + (char)(28) + (char)(28) + "/" + "-"
        // Long: 72+28=100('d'), 1+28=29(GS), 75+28=103('g').
        // SP=28->0, Shared=28->0, DC=28->0.
        // Sym: /-
        
        string dest = "111111"; // South, East
        char d = (char)(10 + 28); // 10 deg East
        char m = (char)(20 + 28); // 20 min
        char h = (char)(50 + 28); // 50 hun -> 20.50 min
        char sp = (char)(0 + 28); // 0
        char sh = (char)(0 + 28);
        char dc = (char)(0 + 28);
        // In Mic-E, byte 7 is symbol code, byte 8 is symbol table
        // So for symbol code '-' and table '/', we put code first, then table
        string payload = "`" + d + m + h + sp + sh + dc + "-/";
        
        string packetRaw = $"N0CALL>{dest}:{payload}";
        
        var packet = _parser.Parse(packetRaw);
        
        packet.Type.Should().Be(PacketType.MicE);
        packet.Position!.Latitude.Should().BeApproximately(-(11 + 11.11/60.0), 0.0001); // South
        packet.Position!.Longitude.Should().BeApproximately(10 + 20.50/60.0, 0.0001); // East
        packet.SymbolTable.Should().Be("/");
        packet.SymbolCode.Should().Be("-");
    }
}
