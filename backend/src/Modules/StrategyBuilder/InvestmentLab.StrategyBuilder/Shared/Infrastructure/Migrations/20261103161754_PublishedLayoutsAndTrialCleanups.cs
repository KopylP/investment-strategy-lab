using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.StrategyBuilder.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PublishedLayoutsAndTrialCleanups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "published_layouts",
                schema: "strategy_builder",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    strategy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    graph = table.Column<string>(type: "jsonb", nullable: false),
                    layout = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_published_layouts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trial_cleanups",
                schema: "strategy_builder",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trial_strategy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trial_cleanups", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_published_layouts_strategy_id_version",
                schema: "strategy_builder",
                table: "published_layouts",
                columns: new[] { "strategy_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trial_cleanups_next_attempt_at",
                schema: "strategy_builder",
                table: "trial_cleanups",
                column: "next_attempt_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "published_layouts",
                schema: "strategy_builder");

            migrationBuilder.DropTable(
                name: "trial_cleanups",
                schema: "strategy_builder");
        }
    }
}
