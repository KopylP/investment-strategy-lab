using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Portfolio.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ForeignCash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_holdings_account_id_sub_portfolio_id_instrument_id",
                schema: "portfolio",
                table: "holdings");

            migrationBuilder.CreateIndex(
                name: "ix_holdings_account_id_sub_portfolio_id_instrument_id_currency",
                schema: "portfolio",
                table: "holdings",
                columns: new[] { "account_id", "sub_portfolio_id", "instrument_id", "currency" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_holdings_account_id_sub_portfolio_id_instrument_id_currency",
                schema: "portfolio",
                table: "holdings");

            migrationBuilder.CreateIndex(
                name: "ix_holdings_account_id_sub_portfolio_id_instrument_id",
                schema: "portfolio",
                table: "holdings",
                columns: new[] { "account_id", "sub_portfolio_id", "instrument_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }
    }
}
