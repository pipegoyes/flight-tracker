using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTargetDateDestinations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TargetDateDestinations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TargetDateId = table.Column<int>(type: "INTEGER", nullable: false),
                    DestinationId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TargetDateDestinations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TargetDateDestinations_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TargetDateDestinations_TargetDates_TargetDateId",
                        column: x => x.TargetDateId,
                        principalTable: "TargetDates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TargetDateDestinations_DestinationId",
                table: "TargetDateDestinations",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_TargetDateDestinations_TargetDateId_DestinationId",
                table: "TargetDateDestinations",
                columns: new[] { "TargetDateId", "DestinationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TargetDateDestinations");
        }
    }
}
