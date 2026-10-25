using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.MarketData.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_quotes",
                schema: "marketdata",
                columns: table => new
                {
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_date = table.Column<DateOnly>(type: "date", nullable: false),
                    close = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    adjusted_close = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fetched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    quality_flags = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_quotes", x => new { x.instrument_id, x.trading_date });
                });

            migrationBuilder.CreateTable(
                name: "quote_corrections",
                schema: "marketdata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_date = table.Column<DateOnly>(type: "date", nullable: false),
                    close = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    adjusted_close = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    corrected_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    corrected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quote_corrections", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_quote_corrections_instrument_id_trading_date",
                schema: "marketdata",
                table: "quote_corrections",
                columns: new[] { "instrument_id", "trading_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_quotes",
                schema: "marketdata");

            migrationBuilder.DropTable(
                name: "quote_corrections",
                schema: "marketdata");
        }
    }
}
