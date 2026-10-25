using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Portfolio.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "portfolio");

            migrationBuilder.CreateTable(
                name: "accounts",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    broker = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    base_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    tax_policy_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    opened_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    drift_band = table.Column<decimal>(type: "numeric", nullable: false),
                    contribution_policy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "holdings",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sub_portfolio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    lots = table.Column<string>(type: "jsonb", nullable: false),
                    as_of = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_holdings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    occurred_on = table.Column<DateOnly>(type: "date", nullable: false),
                    source_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: false),
                    reversal_of = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transactions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sub_portfolios",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target_share = table.Column<decimal>(type: "numeric", nullable: false),
                    strategy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    strategy_pinned_version = table.Column<int>(type: "integer", nullable: true),
                    cash_instrument_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    max_monthly_turnover = table.Column<decimal>(type: "numeric", nullable: true),
                    max_position_weight = table.Column<decimal>(type: "numeric", nullable: true)
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
                name: "ix_accounts_owner_id",
                schema: "portfolio",
                table: "accounts",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_holdings_account_id_sub_portfolio_id_instrument_id",
                schema: "portfolio",
                table: "holdings",
                columns: new[] { "account_id", "sub_portfolio_id", "instrument_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_occurred_at",
                schema: "portfolio",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sub_portfolios_account_id_position",
                schema: "portfolio",
                table: "sub_portfolios",
                columns: new[] { "account_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_transactions_account_id_occurred_on",
                schema: "portfolio",
                table: "transactions",
                columns: new[] { "account_id", "occurred_on" });

            migrationBuilder.CreateIndex(
                name: "ix_transactions_reversal_of",
                schema: "portfolio",
                table: "transactions",
                column: "reversal_of",
                unique: true,
                filter: "reversal_of IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_source_reference",
                schema: "portfolio",
                table: "transactions",
                column: "source_reference",
                unique: true,
                filter: "source_reference IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "holdings",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "sub_portfolios",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "transactions",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "accounts",
                schema: "portfolio");
        }
    }
}
