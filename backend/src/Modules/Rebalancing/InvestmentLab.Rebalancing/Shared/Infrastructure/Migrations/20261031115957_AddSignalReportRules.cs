using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Rebalancing.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSignalReportRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cause",
                schema: "rebalancing",
                table: "report_requests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "signal_trigger_id",
                schema: "rebalancing",
                table: "report_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "signal_report_rules",
                schema: "rebalancing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    signal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    problem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_signal_report_rules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_report_requests_signal_trigger_id_account_id_code",
                schema: "rebalancing",
                table: "report_requests",
                columns: new[] { "signal_trigger_id", "account_id", "code" },
                unique: true,
                filter: "signal_trigger_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_signal_report_rules_signal_id_account_id_report_code",
                schema: "rebalancing",
                table: "signal_report_rules",
                columns: new[] { "signal_id", "account_id", "report_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "signal_report_rules",
                schema: "rebalancing");

            migrationBuilder.DropIndex(
                name: "ix_report_requests_signal_trigger_id_account_id_code",
                schema: "rebalancing",
                table: "report_requests");

            migrationBuilder.DropColumn(
                name: "cause",
                schema: "rebalancing",
                table: "report_requests");

            migrationBuilder.DropColumn(
                name: "signal_trigger_id",
                schema: "rebalancing",
                table: "report_requests");
        }
    }
}
