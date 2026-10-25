using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InvestmentLab.Performance.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "performance");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "performance",
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
                name: "planned_reports",
                schema: "performance",
                columns: table => new
                {
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    period = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    market_as_of = table.Column<DateOnly>(type: "date", nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actions = table.Column<string>(type: "jsonb", nullable: false),
                    is_superseded = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_planned_reports", x => x.report_id);
                });

            migrationBuilder.CreateTable(
                name: "recalculation_markers",
                schema: "performance",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dirty_from = table.Column<DateOnly>(type: "date", nullable: true),
                    built_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    built_through = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recalculation_markers", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "tax_year_totals",
                schema: "performance",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_due_pln = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tax_year_totals", x => new { x.account_id, x.year });
                });

            migrationBuilder.CreateTable(
                name: "valuation_snapshots",
                schema: "performance",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scenario = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sub_portfolio_id = table.Column<Guid>(type: "uuid", nullable: true),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    value = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    cash = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    flow = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    net_contributions = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    twr_index = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_valuation_snapshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_occurred_at",
                schema: "performance",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_planned_reports_account_id",
                schema: "performance",
                table: "planned_reports",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_valuation_snapshots_account_id_scenario_date",
                schema: "performance",
                table: "valuation_snapshots",
                columns: new[] { "account_id", "scenario", "date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "performance");

            migrationBuilder.DropTable(
                name: "planned_reports",
                schema: "performance");

            migrationBuilder.DropTable(
                name: "recalculation_markers",
                schema: "performance");

            migrationBuilder.DropTable(
                name: "tax_year_totals",
                schema: "performance");

            migrationBuilder.DropTable(
                name: "valuation_snapshots",
                schema: "performance");
        }
    }
}
