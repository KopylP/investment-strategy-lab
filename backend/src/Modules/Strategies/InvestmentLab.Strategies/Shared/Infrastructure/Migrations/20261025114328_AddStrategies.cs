using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Strategies.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStrategies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "strategies");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "strategies",
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
                name: "strategies",
                schema: "strategies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_strategies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "strategy_types",
                schema: "strategies",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    engine = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    engine_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    supports_state = table.Column<bool>(type: "boolean", nullable: false),
                    required_series_roles = table.Column<List<string>>(type: "text[]", nullable: false),
                    parameters_schema = table.Column<string>(type: "jsonb", nullable: false),
                    default_parameters = table.Column<string>(type: "jsonb", nullable: false),
                    synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_strategy_types", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "strategy_versions",
                schema: "strategies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    parameters = table.Column<string>(type: "jsonb", nullable: false),
                    universe = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    cash_instrument_id = table.Column<Guid>(type: "uuid", nullable: true),
                    benchmark_instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    volatility_index_instrument_id = table.Column<Guid>(type: "uuid", nullable: true),
                    frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    strategy_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_strategy_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_strategy_versions_strategies_strategy_id",
                        column: x => x.strategy_id,
                        principalSchema: "strategies",
                        principalTable: "strategies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_occurred_at",
                schema: "strategies",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_strategies_owner_id_name",
                schema: "strategies",
                table: "strategies",
                columns: new[] { "owner_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_strategy_versions_strategy_id_number",
                schema: "strategies",
                table: "strategy_versions",
                columns: new[] { "strategy_id", "number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "strategies");

            migrationBuilder.DropTable(
                name: "strategy_types",
                schema: "strategies");

            migrationBuilder.DropTable(
                name: "strategy_versions",
                schema: "strategies");

            migrationBuilder.DropTable(
                name: "strategies",
                schema: "strategies");
        }
    }
}
