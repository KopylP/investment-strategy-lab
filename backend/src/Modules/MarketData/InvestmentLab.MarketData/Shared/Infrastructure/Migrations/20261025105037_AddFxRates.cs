using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.MarketData.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFxRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fx_rates",
                schema: "marketdata",
                columns: table => new
                {
                    source = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    @base = table.Column<string>(name: "base", type: "character varying(3)", maxLength: 3, nullable: false),
                    quote = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    rate = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    table_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fx_rates", x => new { x.source, x.@base, x.quote, x.effective_date });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fx_rates",
                schema: "marketdata");
        }
    }
}
