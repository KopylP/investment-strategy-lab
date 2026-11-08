using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Identity.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProfileLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "locale",
                schema: "identity",
                table: "user_profiles");

            migrationBuilder.AddColumn<string>(
                name: "language",
                schema: "identity",
                table: "user_profiles",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "language",
                schema: "identity",
                table: "user_profiles");

            migrationBuilder.AddColumn<string>(
                name: "locale",
                schema: "identity",
                table: "user_profiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "uk-UA");
        }
    }
}
