using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.MarketData.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropStoredLongQuoteHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM marketdata.daily_quotes AS quote
                USING marketdata.instruments AS instrument
                WHERE quote.instrument_id = instrument.id
                  AND quote.fetched_at = instrument.full_quote_history_fetched_at;
                """);

            migrationBuilder.DropColumn(
                name: "full_quote_history_fetched_at",
                schema: "marketdata",
                table: "instruments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "full_quote_history_fetched_at",
                schema: "marketdata",
                table: "instruments",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
