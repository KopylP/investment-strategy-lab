using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvestmentLab.Taxation.Shared.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompressLargeJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    ALTER TABLE taxation.assessments ALTER COLUMN realizations SET COMPRESSION lz4;
                EXCEPTION WHEN feature_not_supported THEN NULL;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    ALTER TABLE taxation.broker_statements ALTER COLUMN lots SET COMPRESSION lz4;
                EXCEPTION WHEN feature_not_supported THEN NULL;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    ALTER TABLE taxation.assessments ALTER COLUMN realizations SET COMPRESSION DEFAULT;
                EXCEPTION WHEN feature_not_supported THEN NULL;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    ALTER TABLE taxation.broker_statements ALTER COLUMN lots SET COMPRESSION DEFAULT;
                EXCEPTION WHEN feature_not_supported THEN NULL;
                END $$;
                """);
        }
    }
}
