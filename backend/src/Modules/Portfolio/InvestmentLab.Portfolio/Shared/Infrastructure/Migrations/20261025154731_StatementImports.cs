using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Portfolio.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StatementImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "statement_imports",
                schema: "portfolio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    broker = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: true),
                    cash_sub_portfolio_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rows = table.Column<string>(type: "jsonb", nullable: false),
                    mappings = table.Column<string>(type: "jsonb", nullable: false),
                    warnings = table.Column<string>(type: "jsonb", nullable: false),
                    outcomes = table.Column<string>(type: "jsonb", nullable: false),
                    recorded_references = table.Column<string>(type: "jsonb", nullable: false),
                    reversed_transaction_ids = table.Column<string>(type: "jsonb", nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    undone_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_statement_imports", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_statement_imports_account_id",
                schema: "portfolio",
                table: "statement_imports",
                column: "account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "statement_imports",
                schema: "portfolio");
        }
    }
}
