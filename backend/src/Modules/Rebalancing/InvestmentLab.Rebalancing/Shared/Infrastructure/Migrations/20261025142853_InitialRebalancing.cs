using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Rebalancing.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialRebalancing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "rebalancing");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "rebalancing",
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
                name: "plan_settings",
                schema: "rebalancing",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    monthly_contribution = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    min_trade = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    trim_band = table.Column<decimal>(type: "numeric", nullable: false),
                    allow_fractional_shares = table.Column<bool>(type: "boolean", nullable: false),
                    generate_on_schedule = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_settings", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "rebalancing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    period = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    trigger = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    market_as_of = table.Column<DateOnly>(type: "date", nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    contribution = table.Column<string>(type: "jsonb", nullable: false),
                    sections = table.Column<string>(type: "jsonb", nullable: false),
                    warnings = table.Column<string>(type: "jsonb", nullable: false),
                    review = table.Column<string>(type: "jsonb", nullable: true),
                    superseded_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_actions",
                schema: "rebalancing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: true),
                    planned_quantity = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    planned_price = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    planned_amount = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    allocations = table.Column<string>(type: "jsonb", nullable: false),
                    from_sub_portfolio_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_sub_portfolio_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fills = table.Column<string>(type: "jsonb", nullable: false),
                    next_fill_number = table.Column<int>(type: "integer", nullable: false),
                    skip_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    rejection = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    report_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_actions", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_actions_reports_report_id",
                        column: x => x.report_id,
                        principalSchema: "rebalancing",
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_occurred_at",
                schema: "rebalancing",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_report_actions_report_id",
                schema: "rebalancing",
                table: "report_actions",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_account_id_period",
                schema: "rebalancing",
                table: "reports",
                columns: new[] { "account_id", "period" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "rebalancing");

            migrationBuilder.DropTable(
                name: "plan_settings",
                schema: "rebalancing");

            migrationBuilder.DropTable(
                name: "report_actions",
                schema: "rebalancing");

            migrationBuilder.DropTable(
                name: "reports",
                schema: "rebalancing");
        }
    }
}
