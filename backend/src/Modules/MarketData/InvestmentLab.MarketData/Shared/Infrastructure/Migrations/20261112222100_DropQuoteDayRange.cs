using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.MarketData.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropQuoteDayRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "high",
                schema: "marketdata",
                table: "daily_quotes");

            migrationBuilder.DropColumn(
                name: "low",
                schema: "marketdata",
                table: "daily_quotes");

            migrationBuilder.DropColumn(
                name: "open",
                schema: "marketdata",
                table: "daily_quotes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "high",
                schema: "marketdata",
                table: "daily_quotes",
                type: "numeric(20,8)",
                precision: 20,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "low",
                schema: "marketdata",
                table: "daily_quotes",
                type: "numeric(20,8)",
                precision: 20,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "open",
                schema: "marketdata",
                table: "daily_quotes",
                type: "numeric(20,8)",
                precision: 20,
                scale: 8,
                nullable: true);
        }
    }
}
