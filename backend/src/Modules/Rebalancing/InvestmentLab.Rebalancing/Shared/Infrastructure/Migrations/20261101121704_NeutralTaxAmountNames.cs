using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Rebalancing.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NeutralTaxAmountNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "estimated_tax_pln",
                schema: "rebalancing",
                table: "report_actions",
                newName: "estimated_tax_change");

            migrationBuilder.RenameColumn(
                name: "estimated_gain_pln",
                schema: "rebalancing",
                table: "report_actions",
                newName: "estimated_gain");

            migrationBuilder.RenameColumn(
                name: "estimated_carried_loss_pln",
                schema: "rebalancing",
                table: "report_actions",
                newName: "estimated_carried_loss");

            migrationBuilder.Sql("""
                UPDATE rebalancing.reports
                SET tax_estimate = replace(replace(replace(replace(tax_estimate::text,
                    '"gainPln"', '"gainInTaxCurrency"'),
                    '"taxPln"', '"taxChange"'),
                    '"yearTaxBeforePln"', '"yearTaxBefore"'),
                    '"carriedLossPln"', '"carriedLoss"')::jsonb
                WHERE tax_estimate IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE rebalancing.reports
                SET tax_estimate = replace(replace(replace(replace(tax_estimate::text,
                    '"gainInTaxCurrency"', '"gainPln"'),
                    '"taxChange"', '"taxPln"'),
                    '"yearTaxBefore"', '"yearTaxBeforePln"'),
                    '"carriedLoss"', '"carriedLossPln"')::jsonb
                WHERE tax_estimate IS NOT NULL;
                """);

            migrationBuilder.RenameColumn(
                name: "estimated_tax_change",
                schema: "rebalancing",
                table: "report_actions",
                newName: "estimated_tax_pln");

            migrationBuilder.RenameColumn(
                name: "estimated_gain",
                schema: "rebalancing",
                table: "report_actions",
                newName: "estimated_gain_pln");

            migrationBuilder.RenameColumn(
                name: "estimated_carried_loss",
                schema: "rebalancing",
                table: "report_actions",
                newName: "estimated_carried_loss_pln");
        }
    }
}
