using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Performance.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshotCostBasis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "cost_basis",
                schema: "performance",
                table: "valuation_snapshots",
                type: "numeric(20,8)",
                precision: 20,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            // Snapshots already stored have no cost basis: every account rebuilds them on its next read.
            migrationBuilder.Sql("UPDATE performance.recalculation_markers SET dirty_from = '0001-01-01';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cost_basis",
                schema: "performance",
                table: "valuation_snapshots");
        }
    }
}
