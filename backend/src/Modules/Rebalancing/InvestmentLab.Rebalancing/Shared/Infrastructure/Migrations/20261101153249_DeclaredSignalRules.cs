using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Rebalancing.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeclaredSignalRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "dismissed_at",
                schema: "rebalancing",
                table: "signal_report_rules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "from_strategy",
                schema: "rebalancing",
                table: "signal_report_rules",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dismissed_at",
                schema: "rebalancing",
                table: "signal_report_rules");

            migrationBuilder.DropColumn(
                name: "from_strategy",
                schema: "rebalancing",
                table: "signal_report_rules");
        }
    }
}
