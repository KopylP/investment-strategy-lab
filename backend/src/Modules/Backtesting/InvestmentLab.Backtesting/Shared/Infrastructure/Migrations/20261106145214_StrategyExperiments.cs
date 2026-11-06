using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Backtesting.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StrategyExperiments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "experiments",
                schema: "backtesting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    strategy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    strategy_version = table.Column<int>(type: "integer", nullable: false),
                    conditions = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    result = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_experiments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_experiments_strategy_id_strategy_version",
                schema: "backtesting",
                table: "experiments",
                columns: new[] { "strategy_id", "strategy_version" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "experiments",
                schema: "backtesting");
        }
    }
}
