using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Rebalancing.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReportActionCarriedLoss : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "estimated_carried_loss_pln",
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
                name: "estimated_carried_loss_pln",
                schema: "rebalancing",
                table: "report_actions");
        }
    }
}
