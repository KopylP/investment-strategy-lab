using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Portfolio.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SubPortfoliosAsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "sub_portfolios",
                schema: "portfolio",
                table: "accounts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE portfolio.accounts AS account
                SET sub_portfolios = COALESCE((
                    SELECT jsonb_agg(
                        jsonb_build_object(
                            'Id', sub.id,
                            'CashInstrumentId', sub.cash_instrument_id,
                            'Guardrails', jsonb_build_object(
                                'MaxMonthlyTurnover', sub.max_monthly_turnover,
                                'MaxPositionWeight', sub.max_position_weight),
                            'Name', sub.name,
                            'Position', sub.position,
                            'Status', sub.status,
                            'Strategy', CASE
                                WHEN sub.strategy_id IS NULL THEN NULL
                                ELSE jsonb_build_object('PinnedVersion', sub.strategy_pinned_version, 'StrategyId', sub.strategy_id)
                            END,
                            'TargetShare', sub.target_share)
                        ORDER BY sub.position)
                    FROM portfolio.sub_portfolios AS sub
                    WHERE sub.account_id = account.id), '[]'::jsonb)
                """);

            migrationBuilder.DropTable(
                name: "sub_portfolios",
                schema: "portfolio");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sub_portfolios",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cash_instrument_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_share = table.Column<decimal>(type: "numeric", nullable: false),
                    max_monthly_turnover = table.Column<decimal>(type: "numeric", nullable: true),
                    max_position_weight = table.Column<decimal>(type: "numeric", nullable: true),
                    strategy_pinned_version = table.Column<int>(type: "integer", nullable: true),
                    strategy_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sub_portfolios", x => x.id);
                    table.ForeignKey(
                        name: "fk_sub_portfolios_accounts_account_id",
                        column: x => x.account_id,
                        principalSchema: "portfolio",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sub_portfolios_account_id_position",
                schema: "portfolio",
                table: "sub_portfolios",
                columns: new[] { "account_id", "position" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO portfolio.sub_portfolios (
                    id, account_id, cash_instrument_id, name, position, status, target_share,
                    max_monthly_turnover, max_position_weight, strategy_pinned_version, strategy_id)
                SELECT
                    (element ->> 'Id')::uuid,
                    account.id,
                    (element ->> 'CashInstrumentId')::uuid,
                    element ->> 'Name',
                    (element ->> 'Position')::integer,
                    element ->> 'Status',
                    (element ->> 'TargetShare')::numeric,
                    (element -> 'Guardrails' ->> 'MaxMonthlyTurnover')::numeric,
                    (element -> 'Guardrails' ->> 'MaxPositionWeight')::numeric,
                    (element -> 'Strategy' ->> 'PinnedVersion')::integer,
                    (element -> 'Strategy' ->> 'StrategyId')::uuid
                FROM portfolio.accounts AS account
                CROSS JOIN LATERAL jsonb_array_elements(COALESCE(account.sub_portfolios, '[]'::jsonb)) AS element
                """);

            migrationBuilder.DropColumn(
                name: "sub_portfolios",
                schema: "portfolio",
                table: "accounts");
        }
    }
}
