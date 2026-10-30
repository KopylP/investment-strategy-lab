using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Strategies.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReportSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // An etf-momentum strategy's "reporting.rebalance" and "reporting.riskReview" grow from a frequency into a schedule
            // (frequency, day, weekday, time, minute), at the times the reports came so far.
            foreach (var (name, time) in new[] { ("rebalance", "20:00"), ("riskReview", "19:00") })
            {
                migrationBuilder.Sql($$"""
                    UPDATE strategies.strategy_versions AS v
                    SET parameters = jsonb_set(v.parameters, '{reporting,{{name}}}', jsonb_build_object(
                        'frequency', v.parameters #>> '{reporting,{{name}}}', 'day', 1, 'weekday', 'sunday', 'time', '{{time}}', 'minute', 0))
                    FROM strategies.strategies AS s
                    WHERE s.id = v.strategy_id AND s.type_code = 'etf-momentum'
                      AND jsonb_typeof(v.parameters #> '{reporting,{{name}}}') = 'string';
                    """);
            }

            // Synced again on the next read, so the engine's report schedules arrive at once.
            migrationBuilder.Sql("UPDATE strategies.strategy_types SET synced_at = '2000-01-01T00:00:00Z';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Back to a bare frequency; hourly and daily did not exist, the nearest is weekly.
            foreach (var name in new[] { "rebalance", "riskReview" })
            {
                migrationBuilder.Sql($$"""
                    UPDATE strategies.strategy_versions
                    SET parameters = jsonb_set(parameters, '{reporting,{{name}}}', to_jsonb(CASE
                        WHEN parameters #>> '{reporting,{{name}},frequency}' IN ('hourly', 'daily') THEN 'weekly'
                        ELSE parameters #>> '{reporting,{{name}},frequency}' END))
                    WHERE jsonb_typeof(parameters #> '{reporting,{{name}}}') = 'object';
                    """);
            }

            migrationBuilder.Sql("UPDATE strategies.strategy_types SET synced_at = '2000-01-01T00:00:00Z';");
        }
    }
}
