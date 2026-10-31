using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Signals.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "signals");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "signals",
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
                name: "signals",
                schema: "signals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target = table.Column<string>(type: "jsonb", nullable: false),
                    condition = table.Column<string>(type: "jsonb", nullable: false),
                    schedule = table.Column<string>(type: "jsonb", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    built_in_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_checked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_signals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "signal_occurrences",
                schema: "signals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    signal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trigger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    subject_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    trigger_value = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    trigger_date = table.Column<DateOnly>(type: "date", nullable: false),
                    triggered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    latest_value = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    latest_date = table.Column<DateOnly>(type: "date", nullable: false),
                    cleared_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    clear_reason = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_signal_occurrences", x => x.id);
                    table.ForeignKey(
                        name: "fk_signal_occurrences_signals_signal_id",
                        column: x => x.signal_id,
                        principalSchema: "signals",
                        principalTable: "signals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_occurred_at",
                schema: "signals",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_signal_occurrences_signal_id_subject_key",
                schema: "signals",
                table: "signal_occurrences",
                columns: new[] { "signal_id", "subject_key" },
                unique: true,
                filter: "cleared_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_signal_occurrences_triggered_at",
                schema: "signals",
                table: "signal_occurrences",
                column: "triggered_at");

            migrationBuilder.CreateIndex(
                name: "ix_signals_built_in_key",
                schema: "signals",
                table: "signals",
                column: "built_in_key",
                unique: true,
                filter: "built_in_key IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "signals");

            migrationBuilder.DropTable(
                name: "signal_occurrences",
                schema: "signals");

            migrationBuilder.DropTable(
                name: "signals",
                schema: "signals");
        }
    }
}
