using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Rebalancing.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReportCodeAndTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "code",
                schema: "rebalancing",
                table: "reports",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "title",
                schema: "rebalancing",
                table: "reports",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Reports from before strategies declared their own: the two the etf-momentum strategies declare now.
            migrationBuilder.Sql("""
                UPDATE rebalancing.reports
                SET code = CASE WHEN kind = 'RiskReview' THEN 'risk-review' ELSE 'rebalance' END,
                    title = CASE WHEN kind = 'RiskReview' THEN 'Перевірка ризику' ELSE 'Ребаланс' END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "code",
                schema: "rebalancing",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "title",
                schema: "rebalancing",
                table: "reports");
        }
    }
}
