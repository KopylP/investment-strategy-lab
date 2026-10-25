using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Taxation.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialTaxation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "taxation");

            migrationBuilder.CreateTable(
                name: "accounts",
                schema: "taxation",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    policy_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounts", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "assessments",
                schema: "taxation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    policy_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    policy_version = table.Column<int>(type: "integer", nullable: false),
                    input_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    computed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    summary = table.Column<string>(type: "jsonb", nullable: false),
                    realizations = table.Column<string>(type: "jsonb", nullable: false),
                    dividends = table.Column<string>(type: "jsonb", nullable: false),
                    carryforward_uses = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assessments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ledger_entries",
                schema: "taxation",
                columns: table => new
                {
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_on = table.Column<DateOnly>(type: "date", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    instrument_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    price = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    fee = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    allocations = table.Column<string>(type: "jsonb", nullable: false),
                    is_reversed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledger_entries", x => x.transaction_id);
                });

            migrationBuilder.CreateTable(
                name: "loss_carryforwards",
                schema: "taxation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    already_used = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loss_carryforwards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "taxation",
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

            migrationBuilder.CreateIndex(
                name: "ix_assessments_account_id_year",
                schema: "taxation",
                table: "assessments",
                columns: new[] { "account_id", "year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_account_id",
                schema: "taxation",
                table: "ledger_entries",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_loss_carryforwards_account_id",
                schema: "taxation",
                table: "loss_carryforwards",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_occurred_at",
                schema: "taxation",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounts",
                schema: "taxation");

            migrationBuilder.DropTable(
                name: "assessments",
                schema: "taxation");

            migrationBuilder.DropTable(
                name: "ledger_entries",
                schema: "taxation");

            migrationBuilder.DropTable(
                name: "loss_carryforwards",
                schema: "taxation");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "taxation");
        }
    }
}
