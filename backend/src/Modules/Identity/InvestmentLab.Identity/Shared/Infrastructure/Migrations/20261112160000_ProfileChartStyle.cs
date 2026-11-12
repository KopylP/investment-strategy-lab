using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Identity.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProfileChartStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "chart_style",
                schema: "identity",
                table: "user_profiles",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Line");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "chart_style",
                schema: "identity",
                table: "user_profiles");
        }
    }
}
