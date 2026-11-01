using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Strategies.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StrategyTypeMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "messages",
                schema: "strategies",
                table: "strategy_types",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.Sql("UPDATE strategies.strategy_types SET synced_at = '2000-01-01T00:00:00Z';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "messages",
                schema: "strategies",
                table: "strategy_types");
        }
    }
}
