using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Rebalancing.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReportTaxEstimate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tax_estimate",
                schema: "rebalancing",
                table: "reports",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "estimated_gain_pln",
                schema: "rebalancing",
                table: "report_actions",
                type: "numeric(20,8)",
                precision: 20,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "estimated_tax_pln",
                schema: "rebalancing",
                table: "report_actions",
                type: "numeric(20,8)",
                precision: 20,
                scale: 8,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tax_estimate",
                schema: "rebalancing",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "estimated_gain_pln",
                schema: "rebalancing",
                table: "report_actions");

            migrationBuilder.DropColumn(
                name: "estimated_tax_pln",
                schema: "rebalancing",
                table: "report_actions");
        }
    }
}
