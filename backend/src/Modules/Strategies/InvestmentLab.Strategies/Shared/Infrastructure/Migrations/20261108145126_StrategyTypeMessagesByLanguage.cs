using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Strategies.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StrategyTypeMessagesByLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE strategies.strategy_types SET messages = jsonb_build_object('uk', messages);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE strategies.strategy_types SET messages = COALESCE(messages -> 'uk', '{}'::jsonb);");
        }
    }
}
