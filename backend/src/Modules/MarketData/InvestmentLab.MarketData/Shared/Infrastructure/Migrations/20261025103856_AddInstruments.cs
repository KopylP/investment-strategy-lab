using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.MarketData.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstruments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "marketdata");

            migrationBuilder.CreateTable(
                name: "instruments",
                schema: "marketdata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    isin = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    exchange = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    trading_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    group = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_accumulating = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instruments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "marketdata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "instrument_provider_symbols",
                schema: "marketdata",
                columns: table => new
                {
                    provider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instrument_provider_symbols", x => new { x.instrument_id, x.provider });
                    table.ForeignKey(
                        name: "fk_instrument_provider_symbols_instruments_instrument_id",
                        column: x => x.instrument_id,
                        principalSchema: "marketdata",
                        principalTable: "instruments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_instrument_provider_symbols_provider_symbol",
                schema: "marketdata",
                table: "instrument_provider_symbols",
                columns: new[] { "provider", "symbol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_instruments_isin",
                schema: "marketdata",
                table: "instruments",
                column: "isin",
                unique: true,
                filter: "isin IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_occurred_at",
                schema: "marketdata",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "instrument_provider_symbols",
                schema: "marketdata");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "marketdata");

            migrationBuilder.DropTable(
                name: "instruments",
                schema: "marketdata");
        }
    }
}
