using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.MarketData.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstrumentExposure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "asset_class",
                schema: "marketdata",
                table: "instruments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "asset_currency",
                schema: "marketdata",
                table: "instruments",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "region",
                schema: "marketdata",
                table: "instruments",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "asset_class",
                schema: "marketdata",
                table: "instruments");

            migrationBuilder.DropColumn(
                name: "asset_currency",
                schema: "marketdata",
                table: "instruments");

            migrationBuilder.DropColumn(
                name: "region",
                schema: "marketdata",
                table: "instruments");
        }
    }
}
