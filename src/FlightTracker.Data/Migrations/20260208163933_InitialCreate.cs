using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Destinations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AirportCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Destinations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TargetDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OutboundDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetDates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceChecks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TargetDateId = table.Column<int>(type: "INTEGER", nullable: false),
                    DestinationId = table.Column<int>(type: "INTEGER", nullable: false),
                    CheckTimestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false, defaultValue: "EUR"),
                    DepartureTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    ArrivalTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    Airline = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Stops = table.Column<int>(type: "INTEGER", nullable: false),
                    BookingUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceChecks_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriceChecks_TargetDates_TargetDateId",
                        column: x => x.TargetDateId,
                        principalTable: "TargetDates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Destinations_AirportCode",
                table: "Destinations",
                column: "AirportCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceChecks_CheckTimestamp",
                table: "PriceChecks",
                column: "CheckTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_PriceChecks_DestinationId",
                table: "PriceChecks",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceChecks_TargetDateId_DestinationId",
                table: "PriceChecks",
                columns: new[] { "TargetDateId", "DestinationId" });

            migrationBuilder.CreateIndex(
                name: "IX_TargetDates_OutboundDate_ReturnDate",
                table: "TargetDates",
                columns: new[] { "OutboundDate", "ReturnDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceChecks");

            migrationBuilder.DropTable(
                name: "Destinations");

            migrationBuilder.DropTable(
                name: "TargetDates");
        }
    }
}
