using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Portfolio.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompressLargeJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    ALTER TABLE portfolio.statement_imports ALTER COLUMN rows SET COMPRESSION lz4;
                EXCEPTION WHEN feature_not_supported THEN NULL;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    ALTER TABLE portfolio.statement_imports ALTER COLUMN rows SET COMPRESSION DEFAULT;
                EXCEPTION WHEN feature_not_supported THEN NULL;
                END $$;
                """);
        }
    }
}
