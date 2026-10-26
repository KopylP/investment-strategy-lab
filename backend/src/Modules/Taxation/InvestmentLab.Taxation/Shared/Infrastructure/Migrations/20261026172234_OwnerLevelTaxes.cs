using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Taxation.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OwnerLevelTaxes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Per-account years would collide on (owner, year); they are rebuilt from the ledger on the next read or journal change.
            migrationBuilder.Sql("DELETE FROM taxation.assessments;");

            migrationBuilder.DropIndex(
                name: "ix_loss_carryforwards_account_id",
                schema: "taxation",
                table: "loss_carryforwards");

            migrationBuilder.DropIndex(
                name: "ix_assessments_account_id_year",
                schema: "taxation",
                table: "assessments");

            migrationBuilder.DropColumn(
                name: "account_id",
                schema: "taxation",
                table: "loss_carryforwards");

            migrationBuilder.DropColumn(
                name: "account_id",
                schema: "taxation",
                table: "assessments");

            migrationBuilder.CreateTable(
                name: "settings",
                schema: "taxation",
                columns: table => new
                {
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    alert_share = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settings", x => x.owner_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_loss_carryforwards_owner_id",
                schema: "taxation",
                table: "loss_carryforwards",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_assessments_owner_id_year",
                schema: "taxation",
                table: "assessments",
                columns: new[] { "owner_id", "year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "settings",
                schema: "taxation");

            migrationBuilder.DropIndex(
                name: "ix_loss_carryforwards_owner_id",
                schema: "taxation",
                table: "loss_carryforwards");

            migrationBuilder.DropIndex(
                name: "ix_assessments_owner_id_year",
                schema: "taxation",
                table: "assessments");

            migrationBuilder.AddColumn<Guid>(
                name: "account_id",
                schema: "taxation",
                table: "loss_carryforwards",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "account_id",
                schema: "taxation",
                table: "assessments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_loss_carryforwards_account_id",
                schema: "taxation",
                table: "loss_carryforwards",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_assessments_account_id_year",
                schema: "taxation",
                table: "assessments",
                columns: new[] { "account_id", "year" },
                unique: true);
        }
    }
}
