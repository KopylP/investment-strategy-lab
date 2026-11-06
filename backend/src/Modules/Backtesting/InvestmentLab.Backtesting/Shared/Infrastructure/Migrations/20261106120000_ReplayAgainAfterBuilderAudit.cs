using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InvestmentLab.Backtesting.Shared.Infrastructure.Migrations;

[DbContext(typeof(BacktestingDbContext))]
[Migration("20261106120000_ReplayAgainAfterBuilderAudit")]
public sealed class ReplayAgainAfterBuilderAudit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        UPDATE backtesting.backtests
        SET status = 'Queued', result = NULL, failure_reason = NULL,
            finished_at = NULL, requested_at = CURRENT_TIMESTAMP
        WHERE status <> 'NotSupported';
        """);

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Derived results from the old execution rules must not be restored.
    }
}
