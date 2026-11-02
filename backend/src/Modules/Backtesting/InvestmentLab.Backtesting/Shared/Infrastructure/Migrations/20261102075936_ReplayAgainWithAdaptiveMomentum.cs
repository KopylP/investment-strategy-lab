using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Backtesting.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplayAgainWithAdaptiveMomentum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM backtesting.backtests;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
