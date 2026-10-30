using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Strategies.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReportsOutOfParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // When a report comes is the user's choice for the account (rebalancing.plan_settings), not a strategy parameter:
            // an etf-momentum strategy loses "reporting", its type declares only the default schedule.
            migrationBuilder.Sql("""
                UPDATE strategies.strategy_versions AS v
                SET parameters = v.parameters - 'reporting'
                FROM strategies.strategies AS s
                WHERE s.id = v.strategy_id AND s.type_code = 'etf-momentum' AND v.parameters ? 'reporting';
                """);

            // Synced again on the next read, so the engine's report schedules arrive at once.
            migrationBuilder.Sql("UPDATE strategies.strategy_types SET synced_at = '2000-01-01T00:00:00Z';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE strategies.strategy_versions AS v
                SET parameters = v.parameters || '{"reporting": {"rebalance": "monthly", "riskReview": "weekly"}}'::jsonb
                FROM strategies.strategies AS s
                WHERE s.id = v.strategy_id AND s.type_code = 'etf-momentum';
                """);

            migrationBuilder.Sql("UPDATE strategies.strategy_types SET synced_at = '2000-01-01T00:00:00Z';");
        }
    }
}
