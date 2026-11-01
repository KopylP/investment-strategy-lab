using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Rebalancing.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillReportTaxCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE rebalancing.reports
                SET tax_estimate = tax_estimate || '{"taxCurrency":"PLN"}'::jsonb
                WHERE tax_estimate ->> 'isTaxed' = 'true'
                  AND tax_estimate ->> 'taxCurrency' IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
