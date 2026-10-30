using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Strategies.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StrategyTypeReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // How often an etf-momentum strategy rebalances moves from the version into its parameters ("reporting.rebalance").
            migrationBuilder.Sql("""
                UPDATE strategies.strategy_versions AS v
                SET parameters = jsonb_set(
                    v.parameters, '{reporting}',
                    COALESCE(v.parameters -> 'reporting', '{}'::jsonb) || jsonb_build_object('rebalance', lower(v.frequency)))
                FROM strategies.strategies AS s
                WHERE s.id = v.strategy_id AND s.type_code = 'etf-momentum';
                """);

            migrationBuilder.DropColumn(
                name: "frequency",
                schema: "strategies",
                table: "strategy_versions");

            migrationBuilder.AddColumn<string>(
                name: "reports",
                schema: "strategies",
                table: "strategy_types",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            // Synced again on the next read, so the engine's reports arrive at once.
            migrationBuilder.Sql("UPDATE strategies.strategy_types SET synced_at = '2000-01-01T00:00:00Z';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reports",
                schema: "strategies",
                table: "strategy_types");

            migrationBuilder.AddColumn<string>(
                name: "frequency",
                schema: "strategies",
                table: "strategy_versions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Monthly");

            migrationBuilder.Sql("""
                UPDATE strategies.strategy_versions
                SET frequency = initcap(parameters #>> '{reporting,rebalance}'),
                    parameters = parameters #- '{reporting,rebalance}'
                WHERE parameters #>> '{reporting,rebalance}' IS NOT NULL;
                """);
        }
    }
}
