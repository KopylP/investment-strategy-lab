using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.MarketData.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstrumentDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "described_at",
                schema: "marketdata",
                table: "instruments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "marketdata",
                table: "instruments",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "described_at",
                schema: "marketdata",
                table: "instruments");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "marketdata",
                table: "instruments");
        }
    }
}
