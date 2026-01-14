using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Aprs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Packets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sender_callsign = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    sender_base = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    sender_ssid = table.Column<int>(type: "integer", nullable: false),
                    dest_callsign = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    dest_base = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    dest_ssid = table.Column<int>(type: "integer", nullable: true),
                    Path = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    Speed = table.Column<double>(type: "double precision", nullable: true),
                    Course = table.Column<int>(type: "integer", nullable: true),
                    wx_wind_dir = table.Column<int>(type: "integer", nullable: true),
                    wx_wind_speed = table.Column<int>(type: "integer", nullable: true),
                    wx_wind_gust = table.Column<int>(type: "integer", nullable: true),
                    wx_temp = table.Column<int>(type: "integer", nullable: true),
                    wx_rain_1h = table.Column<int>(type: "integer", nullable: true),
                    wx_rain_24h = table.Column<int>(type: "integer", nullable: true),
                    wx_rain_midnight = table.Column<int>(type: "integer", nullable: true),
                    wx_humidity = table.Column<int>(type: "integer", nullable: true),
                    wx_pressure = table.Column<int>(type: "integer", nullable: true),
                    SentTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RawContent = table.Column<string>(type: "text", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    SymbolTable = table.Column<string>(type: "text", nullable: true),
                    SymbolCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Packets_latitude",
                table: "Packets",
                column: "latitude");

            migrationBuilder.CreateIndex(
                name: "IX_Packets_longitude",
                table: "Packets",
                column: "longitude");

            migrationBuilder.CreateIndex(
                name: "IX_Packets_ReceivedAt",
                table: "Packets",
                column: "ReceivedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Packets_sender_callsign",
                table: "Packets",
                column: "sender_callsign");

            migrationBuilder.CreateIndex(
                name: "IX_Packets_Type",
                table: "Packets",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Packets");
        }
    }
}
