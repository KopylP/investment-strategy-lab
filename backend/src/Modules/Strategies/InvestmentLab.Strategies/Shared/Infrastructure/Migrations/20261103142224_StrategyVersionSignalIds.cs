using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Strategies.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StrategyVersionSignalIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<Guid>>(
                name: "signal_ids",
                schema: "strategies",
                table: "strategy_versions",
                type: "uuid[]",
                nullable: false,
                defaultValueSql: "'{}'::uuid[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "signal_ids",
                schema: "strategies",
                table: "strategy_versions");
        }
    }
}
